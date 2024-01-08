﻿using MediatR;

namespace Wordle.Events;

public class RoundEnded : BaseEvent
{
    public Guid SessionId { get; set; }
    public long SessionVersion { get; set; }
    public Guid RoundId { get; set; }
    public long RoundVersion { get; set; }
    
    public RoundEnded(string tenant, Guid sessionId, long sessionVersion, Guid roundId, long roundVersion) : base(tenant)
    {
        SessionId = sessionId;
        SessionVersion = sessionVersion;
        RoundId = roundId;
        RoundVersion = roundVersion;
    }
}