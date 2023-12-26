// See https://aka.ms/new-console-template for more information


using Wordle.Common;


const string TableName = "wordle_dictionary";
    
List<string> TYPES = new List<string>()
{
    "dutch",
    "french",
    "italian",
    "scrabble",
    "syscalls",
    "wordle"
};

var counts = new Dictionary<string, int>();

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

var disallowedCharacters = new List<string>() {",", " ", "."};

await (TYPES.Select(async dict =>
    {
        var lines = File.ReadAllLines($"./dictionaries/{dict}.txt");

        for (int i = 0; i < lines.Length; i++)
        {
            var cleaned = lines[i].Trim().ToUpperInvariant();
            var len = cleaned.Length;
            
            if (len >= 3 && !cleaned.Contains(",") && !disallowedCharacters.Any(x => cleaned.Contains(x)))
            {
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
        }
    })).WaitAll();


foreach (var val in counts)
{
    var split = val.Key.Split("#");

    Console.Out.WriteLine(" _details.Add(new DictionaryDetail()  {DictionaryName = \""+split[0]+"\", RowCount = " + val.Value + ", WordLength = " + split[1]  + " });");
}



