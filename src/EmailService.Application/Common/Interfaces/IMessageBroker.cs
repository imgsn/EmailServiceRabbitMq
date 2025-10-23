namespace EmailService.Application.Common.Interfaces;

public interface IMessageBroker
{
    Task PublishEmailAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
    Task PublishBulkEmailsAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default) where T : class;
}
