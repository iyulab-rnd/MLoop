using CsvHelper;
using CsvHelper.Configuration;
using System.Dynamic;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace MLoop.Utils;

public static class CsvUtils
{
    public static CsvConfiguration GetDefaultConfiguration(bool includeHeader = false)
    {
        return new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = includeHeader,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null,
            IgnoreBlankLines = true
        };
    }

    public static List<dynamic> ParseAndValidateData(string text, CsvConfiguration config)
    {
        using var reader = new StringReader(text);
        using var csv = new CsvReader(reader, config);
        var records = new List<dynamic>();

        string[]? headers = null;

        // Try to read the header if the configuration says there is one
        if (config.HasHeaderRecord)
        {
            if (csv.Read())
            {
                csv.ReadHeader();
                headers = csv.HeaderRecord;
            }
        }

        while (csv.Read())
        {
            dynamic record = new ExpandoObject();
            var recordDict = (IDictionary<string, object?>)record;

            for (int i = 0; i < csv.Parser.Count; i++)
            {
                if (csv.TryGetField<string>(i, out var value))
                {
                    string fieldName = headers != null && i < headers.Length ? headers[i] : $"Field{i}";
                    recordDict[fieldName] = value?.Trim();
                }
            }
            records.Add(record);
        }
        return records;
    }

    public static async Task<int> WriteDataToFileAsync(string path, List<dynamic> records, CsvConfiguration config)
    {
        string content = ConvertRecordsToString(records, config, includeHeader: true);
        await RetryFile.WriteAllTextAsync(path, content);
        return records.Count;
    }

    private static string ConvertRecordsToString(List<dynamic> records, CsvConfiguration config, bool includeHeader = false)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var csvWriter = new CsvWriter(writer, config);

        if (includeHeader && records.Count != 0)
        {
            WriteHeader(csvWriter, records.First());
        }

        foreach (var record in records)
        {
            WriteRecord(csvWriter, record);
        }

        writer.Flush();
        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    private static void WriteHeader(CsvWriter csvWriter, dynamic firstRecord)
    {
        var dict = (IDictionary<string, object>)firstRecord;
        foreach (var key in dict.Keys)
        {
            csvWriter.WriteField(key);
        }
        csvWriter.NextRecord();
    }

    private static void WriteRecord(CsvWriter csvWriter, dynamic record)
    {
        var dict = (IDictionary<string, object>)record;
        foreach (var kvp in dict)
        {
            csvWriter.WriteField(kvp.Value?.ToString());
        }
        csvWriter.NextRecord();
    }

    public static async Task<int> AppendDataToFileAsync(string path, List<dynamic> records, CsvConfiguration config)
    {
        string content = ConvertRecordsToString(records, config);

        // 파일이 존재하고 비어있지 않은 경우 마지막 문자 확인
        if (File.Exists(path) && new FileInfo(path).Length > 0)
        {
            var lastChar = await File.ReadAllTextAsync(path, Encoding.UTF8).ContinueWith(t => t.Result.LastOrDefault());

            // 파일의 마지막 문자가 개행 문자가 아니면 개행 문자 추가
            if (lastChar != '\n' && lastChar != '\r')
            {
                content = Environment.NewLine + content;
            }
        }

        await RetryFile.AppendAllTextAsync(path, content);

        // 파일의 총 레코드 수 계산
        var totalRecordCount = await CountTotalRecordsAsync(path, config);
        return totalRecordCount;
    }

    private static async Task<int> CountTotalRecordsAsync(string path, CsvConfiguration config)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);
        var records = new List<dynamic>();

        await csv.ReadAsync();
        if (config.HasHeaderRecord)
        {
            csv.ReadHeader();
        }

        int count = 0;
        while (await csv.ReadAsync())
        {
            count++;
        }

        return count;
    }


    private static string ConvertRecordsToString(List<dynamic> records, CsvConfiguration config)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var csvWriter = new CsvWriter(writer, config);

        foreach (var record in records)
        {
            WriteRecord(csvWriter, record);
        }

        writer.Flush();
        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    public static List<dynamic> ConvertJsonToCsvRecords(JsonNode json)
    {
        var records = new List<dynamic>();

        if (json is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                if (item == null) continue;
                records.Add(ConvertJsonObjectToDynamic(item));
            }
        }
        else if (json is JsonObject jsonObject)
        {
            records.Add(ConvertJsonObjectToDynamic(jsonObject));
        }
        else
        {
            throw new ArgumentException("Invalid JSON format. Expected array or object.");
        }

        return records;
    }

    private static dynamic ConvertJsonObjectToDynamic(JsonNode jsonNode)
    {
        dynamic record = new ExpandoObject();
        var dict = (IDictionary<string, object>)record;

        if (jsonNode is JsonObject jsonObject)
        {
            foreach (var property in jsonObject)
            {
                dict[property.Key] = property.Value?.ToString() ?? "";
            }
        }

        return record;
    }

    public static bool HasHeader(string csvContent)
    {
        using var reader = new StringReader(csvContent);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            HeaderValidated = null,
            MissingFieldFound = null
        });

        try
        {
            // Read the first row
            if (!csv.Read())
            {
                return false; // Not enough data to determine
            }
            var firstRow = GetRowValues(csv);

            // Read the second row
            if (!csv.Read())
            {
                return false; // Not enough data to determine
            }
            var secondRow = GetRowValues(csv);

            if (firstRow.Length != secondRow.Length)
            {
                return false; // Inconsistent column count
            }

            // Check if the first row looks like a header and the second row looks like data
            return IsLikelyHeader(firstRow) && IsLikelyData(secondRow);
        }
        catch (Exception)
        {
            // For any exception, assume no header for safety
            return false;
        }
    }

    private static string[] GetRowValues(CsvReader csv)
    {
        return Enumerable.Range(0, csv.Parser.Count).Select(i => csv.GetField(i)!).ToArray();
    }

    private static bool IsLikelyHeader(string[] row)
    {
        // Check if all values in the row are string-like (not numbers or dates)
        return row.All(value => !double.TryParse(value, out _) && !DateTime.TryParse(value, out _));
    }

    private static bool IsLikelyData(string[] row)
    {
        // Check if at least one value in the row is a number or date
        return row.Any(value => double.TryParse(value, out _) || DateTime.TryParse(value, out _));
    }
}