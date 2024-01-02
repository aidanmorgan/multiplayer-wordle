using Wordle.Common;

namespace DictionaryLoader;

public class DynamoExportFileGenerator : Common
{
    const string TableName = "wordle_dictionary";


    private readonly Dictionary<string, int> counts = new Dictionary<string, int>();


    public async Task Run()
    {
        string OUTPUT_DIR = "./output";

        if (!Directory.Exists(OUTPUT_DIR))
        {
            Directory.CreateDirectory("./output");
        }
        else
        {
            Console.Error.WriteLine("Output directory already exists. Delete directory and start again.");
            return;
        }


        await (DictionaryTypes.Select(async dict =>
        {
            var lines = ReadDictionary(dict);

            for (int i = 0; i < lines.Count; i++)
            {
                var cleaned = lines[i];
                var len = cleaned.Length;

                var key = $"{dict}#{len}";

                if (!counts.ContainsKey(key))
                {
                    counts[key] = 1;
                }
                else
                {
                    var val = counts[key];
                    counts[key] = val + 1;
                }

                var filePath = $"./output/{key}.txt";

                bool exists = File.Exists(filePath);

                await using (var w = File.AppendText(filePath))
                {
                    if (!exists)
                    {
                        await w.WriteLineAsync("pk,sk,word");
                    }

                    await w.WriteLineAsync($"{key},index#{counts[key]},{cleaned}");
                    await w.FlushAsync();
                }

                Console.Out.WriteLine($"{key} added {cleaned}");
            }
        })).WaitAll();


        foreach (var val in counts)
        {
            var split = val.Key.Split("#");

            Console.Out.WriteLine(" _details.Add(new DictionaryDetail()  {DictionaryName = \"" + split[0] +
                                  "\", RowCount = " + val.Value + ", WordLength = " + split[1] + " });");
        }
    }
}