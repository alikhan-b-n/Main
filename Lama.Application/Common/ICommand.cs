namespace Lama.Application.Common;

public interface ICommand<out TResponse>
{
}

public interface ICommand : ICommand<Unit>
{
}

public readonly struct Unit
{
    public static readonly Unit Value = new();
}
