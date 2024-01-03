using Wordle.Model;

namespace Wordle.Persistence.DynamoDb;

public class DynamoGameUnitOfWork : IGameUnitOfWork
{
    private readonly IDynamoMappers _dynamoMappers;
    private readonly DynamoGameConfiguration _configuration;
    
    private DynamoSessionRepository? _sessionRepository;
    public ISessionRepository Sessions => _sessionRepository ??= new DynamoSessionRepository(_dynamoMappers.SessionMapping);

    
    private IDynamoRepository<Guess>? _guessRepository;
    public IRepository<Guess> Guesses => _guessRepository ??= new DynamoRepositoryImpl<Guess>(_dynamoMappers.GuessMapping);

    
    private IDynamoRepository<Round>? _roundRepository;
    public IRepository<Round> Rounds => _roundRepository ??= new DynamoRepositoryImpl<Round>(_dynamoMappers.RoundMapping);

    
    private DynamoOptionsRepository? _optionsRepository;
    public IOptionsRepository Options => _optionsRepository ??= new DynamoOptionsRepository(_dynamoMappers.OptionsMapping);

    public DynamoGameUnitOfWork(DynamoGameConfiguration config, IDynamoMappers dynamoMappers)
    {
        _configuration = config;
        _dynamoMappers = dynamoMappers;
    }
    
    public async Task SaveAsync()
    {
        if (_sessionRepository != null)
        {
            await _sessionRepository.SaveAsync(_configuration);
        }

        if (_guessRepository != null)
        {
            await _guessRepository.SaveAsync(_configuration);
        }

        if (_roundRepository != null)
        {
            await _roundRepository.SaveAsync(_configuration);
        }

        if (_optionsRepository != null)
        {
            await _optionsRepository.SaveAsync(_configuration);
        }
    }
}