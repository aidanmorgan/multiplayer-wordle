// See https://aka.ms/new-console-template for more information


using DictionaryLoader;
using Wordle.Common;

var _mode = Mode.PostgresInsert;

if (_mode == Mode.DynamoFile)
{
    var generator = new DynamoExportFileGenerator();
    await generator.Run();
}
else if (_mode == Mode.PostgresInsert)
{
    var inserter = new PostgresInserter("Host=localhost;Username=wordle;Password=9C7AD426FA34E86B6E4CB576C9754;Database=development_wordle");
    await inserter.Run();
}

enum Mode
{
    DynamoFile,
    PostgresInsert
}