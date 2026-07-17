using FluentValidation;
using MediatR;

namespace AuctionPlatform.Application.Common;

/// <summary>
/// Pipeline behavior MediatR yang otomatis menjalankan semua IValidator&lt;TRequest&gt;
/// terdaftar sebelum request diteruskan ke handler. Kalau ada validation error,
/// request langsung ditolak dengan ValidationException tanpa pernah menyentuh
/// business logic di handler -- jadi command handler bisa asumsikan input
/// sudah valid secara format, dan cukup fokus ke business rule.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}