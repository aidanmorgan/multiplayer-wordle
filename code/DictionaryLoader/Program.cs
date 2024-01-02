// See https://aka.ms/new-console-template for more information


using DictionaryLoader;
using Wordle.Common;

var _mode = Mode.PostgresInsert;

switch (_mode)
{
    case Mode.DynamoFile:
    {
        var generator = new DynamoExportFileGenerator();
        await generator.Run();
        break;
    }
    case Mode.PostgresInsert:
    {
        var inserter = new PostgresInserter("Host=localhost;Username=wordle;Password=9C7AD426FA34E86B6E4CB576C9754;Database=development_wordle");
        await inserter.Run();
        break;
    }
}

enum Mode
{
    DynamoFile,
    PostgresInsert
}