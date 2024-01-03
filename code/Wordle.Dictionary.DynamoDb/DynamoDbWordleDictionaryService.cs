using Amazon.DynamoDBv2.DocumentModel;
using Wordle.Model;

namespace Wordle.Dictionary.DynamoDb;

public class DynamoDbWordleDictionaryService : IWordleDictionaryService
{

    private static string MakeKey(string dictionary, int count)
    {
        return $"{dictionary}#{count}";
    }

    private static readonly List<DictionaryDetail> _details = new List<DictionaryDetail>();

    static DynamoDbWordleDictionaryService()
    {
        var collection = new List<DictionaryDetail>
        {
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 879, WordLength = 3},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 4964, WordLength = 5},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 19452, WordLength = 7},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 27528, WordLength = 8},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 10216, WordLength = 6},
            new DictionaryDetail() {DictionaryName = "italian", RowCount = 6464, WordLength = 6},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 28121, WordLength = 11},
            new DictionaryDetail() {DictionaryName = "italian", RowCount = 3753, WordLength = 5},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 32426, WordLength = 9},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 2525, WordLength = 4},
            new DictionaryDetail() {DictionaryName = "italian", RowCount = 18719, WordLength = 9},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 20976, WordLength = 8},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 26117, WordLength = 10},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 32336, WordLength = 10},
            new DictionaryDetail() {DictionaryName = "scrabble", RowCount = 1015, WordLength = 3},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 21105, WordLength = 12},
            new DictionaryDetail() {DictionaryName = "scrabble", RowCount = 8938, WordLength = 5},
            new DictionaryDetail() {DictionaryName = "italian", RowCount = 15290, WordLength = 8},
            new DictionaryDetail() {DictionaryName = "italian", RowCount = 2977, WordLength = 12},
            new DictionaryDetail() {DictionaryName = "scrabble", RowCount = 15788, WordLength = 6},
            new DictionaryDetail() {DictionaryName = "italian", RowCount = 4886, WordLength = 11},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 13593, WordLength = 13},
            new DictionaryDetail() {DictionaryName = "scrabble", RowCount = 4030, WordLength = 4},
            new DictionaryDetail() {DictionaryName = "italian", RowCount = 11458, WordLength = 7},
            new DictionaryDetail() {DictionaryName = "syscalls", RowCount = 10657, WordLength = 5},
            new DictionaryDetail() {DictionaryName = "scrabble", RowCount = 29766, WordLength = 8},
            new DictionaryDetail() {DictionaryName = "italian", RowCount = 1714, WordLength = 13},
            new DictionaryDetail() {DictionaryName = "scrabble", RowCount = 29150, WordLength = 9},
            new DictionaryDetail() {DictionaryName = "italian", RowCount = 19134, WordLength = 10},
            new DictionaryDetail() {DictionaryName = "wordle", RowCount = 2315, WordLength = 5},
            new DictionaryDetail() {DictionaryName = "scrabble", RowCount = 22326, WordLength = 10},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 15776, WordLength = 7},
            new DictionaryDetail() {DictionaryName = "scrabble", RowCount = 24029, WordLength = 7},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 25034, WordLength = 9},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 8279, WordLength = 14},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 17269, WordLength = 12},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 3469, WordLength = 16},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 22358, WordLength = 11},
            new DictionaryDetail() {DictionaryName = "scrabble", RowCount = 16165, WordLength = 11},
            new DictionaryDetail() {DictionaryName = "scrabble", RowCount = 11417, WordLength = 12},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 12351, WordLength = 13},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 7674, WordLength = 14},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 3699, WordLength = 15},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 5287, WordLength = 15},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 11322, WordLength = 6},
            new DictionaryDetail() {DictionaryName = "scrabble", RowCount = 7750, WordLength = 13},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 1584, WordLength = 16},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 614, WordLength = 17},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 1655, WordLength = 4},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 5031, WordLength = 5},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 1091, WordLength = 18},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 643, WordLength = 19},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 373, WordLength = 20},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 2256, WordLength = 17},
            new DictionaryDetail() {DictionaryName = "italian", RowCount = 1488, WordLength = 4},
            new DictionaryDetail() {DictionaryName = "italian", RowCount = 908, WordLength = 14},
            new DictionaryDetail() {DictionaryName = "scrabble", RowCount = 5059, WordLength = 14},
            new DictionaryDetail() {DictionaryName = "italian", RowCount = 518, WordLength = 15},
            new DictionaryDetail() {DictionaryName = "scrabble", RowCount = 3157, WordLength = 15},
            new DictionaryDetail() {DictionaryName = "italian", RowCount = 431, WordLength = 3},
            new DictionaryDetail() {DictionaryName = "italian", RowCount = 245, WordLength = 16},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 215, WordLength = 18},
            new DictionaryDetail() {DictionaryName = "italian", RowCount = 132, WordLength = 17},
            new DictionaryDetail() {DictionaryName = "italian", RowCount = 45, WordLength = 18},
            new DictionaryDetail() {DictionaryName = "italian", RowCount = 29, WordLength = 19},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 402, WordLength = 3},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 68, WordLength = 19},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 17, WordLength = 20},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 4, WordLength = 21},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 1, WordLength = 25},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 1, WordLength = 22},
            new DictionaryDetail() {DictionaryName = "italian", RowCount = 8, WordLength = 20},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 19, WordLength = 21},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 9, WordLength = 22},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 4, WordLength = 23},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 2, WordLength = 25},
            new DictionaryDetail() {DictionaryName = "french", RowCount = 1, WordLength = 23},
            new DictionaryDetail() {DictionaryName = "dutch", RowCount = 1, WordLength = 24}
        };

        _details = collection.OrderBy(x => x.DictionaryName).ThenBy(x => x.WordLength).ToList();
    }

    private readonly DynamoDictionaryConfiguration _dynamo;
    private readonly Random _random;


    public DynamoDbWordleDictionaryService(DynamoDictionaryConfiguration dynamo)
    {
        _dynamo = dynamo;
        _random = new Random();
    }

    public Task<string> RandomWord(Options opts)
    {
        return RandomWord(opts.DictionaryName, opts.WordLength);
    }

    public async Task<string> RandomWord(string dict, int numLetters)
    {
        var entry = GetEntry(dict, numLetters);

        if (entry != null)
        {
            var table = _dynamo.GetTable();

            int index = _random.Next(0, entry.RowCount);

            var result = await table.Query(new QueryOperationConfig()
            {
                Limit = 1,
                KeyExpression = new Expression()
                {
                    ExpressionStatement = "pk = :pk and sk = :sk",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>()
                    {
                        {":pk", entry.Key},
                        {":sk", $"index#{index}" }
                    }
                },
                CollectResults = true,
                Select = SelectValues.SpecificAttributes,
                AttributesToGet = new List<string>() {"word"}
            }).GetNextSetAsync();

            if (result.Count == 0)
            {
                throw new DictionaryException($"Could not find a word for {entry.Key}");
            }
            else
            {
                return result[0]["word"];
            }
        }
        else
        {
            throw new DictionaryException($"No dictionary: {dict} with length: {numLetters} known.");
        }
    }

    private DictionaryDetail? GetEntry(string dict, int numLetters)
    {
        return _details.FirstOrDefault(x =>
            string.Equals(dict, x.DictionaryName, StringComparison.InvariantCultureIgnoreCase)
            && x.WordLength == numLetters, null);
    }

    public Task<List<DictionaryDetail>> GetDictionaries()
    {
        return Task.FromResult(_details);
    }

    public Task<DictionaryDetail?> GetDictionary(string dictionary, int numLetters)
    {
        return Task.FromResult(GetEntry(dictionary, numLetters));
    }
}