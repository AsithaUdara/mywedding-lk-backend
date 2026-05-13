// File: src/Core/MyWedding.Application/Features/Events/Commands/SetEventPreferences/SetEventPreferencesCommand.cs
using MediatR;
using System;
using System.Collections.Generic;

namespace MyWedding.Events.Application.Features.Events.Commands.SetEventPreferences
{
    public class SetEventPreferencesCommand : IRequest
    {
        public Guid EventId { get; init; }
        public required string UserId { get; init; } // The user making the request
        public required Dictionary<string, string> Preferences { get; init; } // The quiz answers
    }
}
