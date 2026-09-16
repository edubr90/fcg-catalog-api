namespace Catalog.Application.Interfaces;

public interface ISqsPublisher
{
    Task PublishAsync<T>(string queueUrl, T message, CancellationToken cancellationToken = default) where T : class;
}
