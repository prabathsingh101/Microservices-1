using System;

namespace Shared.Contracts;

public interface TestEvent
{
    string Message { get; }
    DateTime Timestamp { get; }
}
