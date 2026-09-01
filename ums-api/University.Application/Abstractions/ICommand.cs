using University.Shared.Common;

namespace University.Application.Abstractions;

public interface ICommand<TResponse> where TResponse : notnull
{
}
