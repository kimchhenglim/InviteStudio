using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace InviteStudio.Web.Helpers;

public static class GuestImportSpreadsheetReader
{
    private static readonly XNamespace SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelationshipNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationshipNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static GuestImportSpreadsheetResult Read(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

        var sharedStrings = ReadSharedStrings(archive);
        var worksheet = LoadFirstWorksheet(archive);
        var rows = worksheet
            .Descendants(SpreadsheetNamespace + "sheetData")
            .Elements(SpreadsheetNamespace + "row")
            .ToList();

        if (rows.Count == 0)
        {
            return new GuestImportSpreadsheetResult(Array.Empty<GuestImportSpreadsheetRow>(), 0);
        }

        var headers = ReadRowValues(rows[0], sharedStrings);
        var nameColumn = FindColumn(headers, "name", "guestname", "guest");
        var phoneColumn = FindColumn(headers, "phone", "phonenumber", "mobile", "mobilenumber");
        var tagColumn = FindColumn(headers, "tag", "tagname", "group");
        var notesColumn = FindColumn(headers, "notes", "note", "remark", "remarks");

        if (nameColumn == null)
        {
            throw new InvalidDataException("The Excel file must contain a 'Name' column.");
        }

        var importedRows = new List<GuestImportSpreadsheetRow>();
        var skippedRowCount = 0;

        foreach (var row in rows.Skip(1))
        {
            var values = ReadRowValues(row, sharedStrings);
            var name = GetValue(values, nameColumn)?.Trim() ?? string.Empty;
            var phoneNumber = GetValue(values, phoneColumn)?.Trim() ?? string.Empty;
            var tag = GetValue(values, tagColumn)?.Trim() ?? string.Empty;
            var notes = GetValue(values, notesColumn)?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name) &&
                string.IsNullOrWhiteSpace(phoneNumber) &&
                string.IsNullOrWhiteSpace(tag) &&
                string.IsNullOrWhiteSpace(notes))
            {
                skippedRowCount++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                skippedRowCount++;
                continue;
            }

            importedRows.Add(new GuestImportSpreadsheetRow(
                RowNumber: GetRowNumber(row),
                Name: name,
                PhoneNumber: phoneNumber,
                Tag: tag,
                Notes: notes));
        }

        return new GuestImportSpreadsheetResult(importedRows, skippedRowCount);
    }

    private static IReadOnlyDictionary<string, string> ReadRowValues(XElement row, IReadOnlyList<string> sharedStrings)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in row.Elements(SpreadsheetNamespace + "c"))
        {
            var reference = cell.Attribute("r")?.Value;
            var column = GetColumnName(reference);
            if (string.IsNullOrWhiteSpace(column))
            {
                continue;
            }

            values[column] = ReadCellValue(cell, sharedStrings);
        }

        return values;
    }

    private static string? FindColumn(IReadOnlyDictionary<string, string> headers, params string[] aliases)
    {
        foreach (var header in headers)
        {
            var normalized = NormalizeHeader(header.Value);
            if (aliases.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                return header.Key;
            }
        }

        return null;
    }

    private static string GetValue(IReadOnlyDictionary<string, string> values, string? columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return string.Empty;
        }

        return values.TryGetValue(columnName, out var value) ? value : string.Empty;
    }

    private static string NormalizeHeader(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private static int GetRowNumber(XElement row)
    {
        return int.TryParse(row.Attribute("r")?.Value, out var rowNumber) ? rowNumber : 0;
    }

    private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var cellType = cell.Attribute("t")?.Value;
        if (string.Equals(cellType, "inlineStr", StringComparison.OrdinalIgnoreCase))
        {
            return string.Concat(cell
                .Descendants(SpreadsheetNamespace + "t")
                .Select(node => node.Value));
        }

        var rawValue = cell.Element(SpreadsheetNamespace + "v")?.Value ?? string.Empty;
        if (string.Equals(cellType, "s", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(rawValue, out var sharedStringIndex) &&
            sharedStringIndex >= 0 &&
            sharedStringIndex < sharedStrings.Count)
        {
            return sharedStrings[sharedStringIndex];
        }

        return rawValue;
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
        {
            return Array.Empty<string>();
        }

        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        return document
            .Descendants(SpreadsheetNamespace + "si")
            .Select(item => string.Concat(item.Descendants(SpreadsheetNamespace + "t").Select(text => text.Value)))
            .ToList();
    }

    private static XDocument LoadFirstWorksheet(ZipArchive archive)
    {
        var workbookEntry = archive.GetEntry("xl/workbook.xml")
            ?? throw new InvalidDataException("The Excel workbook is missing workbook metadata.");
        var workbookRelationshipsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels")
            ?? throw new InvalidDataException("The Excel workbook is missing relationship metadata.");

        using var workbookStream = workbookEntry.Open();
        using var relationshipsStream = workbookRelationshipsEntry.Open();
        var workbook = XDocument.Load(workbookStream);
        var relationships = XDocument.Load(relationshipsStream);

        var firstSheetRelationshipId = workbook
            .Descendants(SpreadsheetNamespace + "sheet")
            .Select(sheet => sheet.Attribute(RelationshipNamespace + "id")?.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (string.IsNullOrWhiteSpace(firstSheetRelationshipId))
        {
            throw new InvalidDataException("The Excel workbook does not contain a worksheet.");
        }

        var target = relationships
            .Descendants(PackageRelationshipNamespace + "Relationship")
            .FirstOrDefault(item => string.Equals(item.Attribute("Id")?.Value, firstSheetRelationshipId, StringComparison.Ordinal))
            ?.Attribute("Target")?.Value;

        var worksheetPath = string.IsNullOrWhiteSpace(target)
            ? "xl/worksheets/sheet1.xml"
            : target.StartsWith("/", StringComparison.Ordinal)
                ? target.TrimStart('/')
                : $"xl/{target.TrimStart('/')}";

        var worksheetEntry = archive.GetEntry(worksheetPath.Replace('\\', '/'))
            ?? throw new InvalidDataException("The Excel workbook worksheet could not be opened.");

        using var worksheetStream = worksheetEntry.Open();
        return XDocument.Load(worksheetStream);
    }

    private static string GetColumnName(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            return string.Empty;
        }

        return new string(cellReference.TakeWhile(char.IsLetter).ToArray());
    }
}

public sealed record GuestImportSpreadsheetRow(
    int RowNumber,
    string Name,
    string PhoneNumber,
    string Tag,
    string Notes);

public sealed record GuestImportSpreadsheetResult(
    IReadOnlyList<GuestImportSpreadsheetRow> Rows,
    int SkippedRowCount);
