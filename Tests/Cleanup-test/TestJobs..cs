// ValidateOrderJob.cs
using FastJobs;
using Microsoft.Extensions.Logging;

public class ValidateOrderJob : IBackGroundJob
{
    private readonly ILogger<ValidateOrderJob> _logger;

    public ValidateOrderJob(ILogger<ValidateOrderJob> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Thread}] ValidateOrderJob Started", Thread.CurrentThread.Name);

        _logger.LogInformation("Step 1: Loading order from database...");
        await Task.Delay(300, cancellationToken);

        _logger.LogInformation("Step 2: Validating order items...");
        var orderItems = Enumerable.Range(1, 5).ToList();
        foreach (var item in orderItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var delay = Random.Shared.Next(80, 200);
            await Task.Delay(delay, cancellationToken);
            _logger.LogInformation("  Validated item #{Item} in {Ms}ms", item, delay);
        }

        _logger.LogInformation("Step 3: Checking stock availability...");
        await Task.Delay(400, cancellationToken);

        _logger.LogInformation("Step 4: Order validation passed.");
        _logger.LogInformation("[{Thread}] ValidateOrderJob Completed", Thread.CurrentThread.Name);
    }
}


// ChargePaymentJob.cs
public class ChargePaymentJob : IBackGroundJob
{
    private readonly ILogger<ChargePaymentJob> _logger;

    public ChargePaymentJob(ILogger<ChargePaymentJob> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Thread}] ChargePaymentJob Started", Thread.CurrentThread.Name);

        _logger.LogInformation("Step 1: Retrieving payment method...");
        await Task.Delay(200, cancellationToken);

        _logger.LogInformation("Step 2: Contacting payment gateway...");
        await Task.Delay(600, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Step 3: Authorizing charge...");
        await Task.Delay(400, cancellationToken);

        _logger.LogInformation("Step 4: Recording transaction to ledger...");
        await Task.Delay(250, cancellationToken);

        _logger.LogInformation("Step 5: Payment of $99.99 charged successfully.");
        _logger.LogInformation("[{Thread}] ChargePaymentJob Completed", Thread.CurrentThread.Name);
    }
}


// SendConfirmationEmailJob.cs
public class SendConfirmationEmailJob : IBackGroundJob
{
    private readonly ILogger<SendConfirmationEmailJob> _logger;

    public SendConfirmationEmailJob(ILogger<SendConfirmationEmailJob> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Thread}] SendConfirmationEmailJob Started", Thread.CurrentThread.Name);

        _logger.LogInformation("Step 1: Loading email template...");
        await Task.Delay(150, cancellationToken);

        _logger.LogInformation("Step 2: Resolving customer email address...");
        await Task.Delay(200, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Step 3: Rendering order summary into template...");
        await Task.Delay(300, cancellationToken);

        _logger.LogInformation("Step 4: Dispatching email via SMTP relay...");
        await Task.Delay(500, cancellationToken);

        _logger.LogInformation("Step 5: Confirmation email sent to customer@example.com.");
        _logger.LogInformation("[{Thread}] SendConfirmationEmailJob Completed", Thread.CurrentThread.Name);
    }
}


// NotifyWarehouseJob.cs
public class NotifyWarehouseJob : IBackGroundJob
{
    private readonly ILogger<NotifyWarehouseJob> _logger;

    public NotifyWarehouseJob(ILogger<NotifyWarehouseJob> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Thread}] NotifyWarehouseJob Started", Thread.CurrentThread.Name);

        _logger.LogInformation("Step 1: Building warehouse dispatch payload...");
        await Task.Delay(200, cancellationToken);

        _logger.LogInformation("Step 2: Connecting to warehouse API...");
        await Task.Delay(350, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Step 3: Sending pick-and-pack instruction...");
        await Task.Delay(450, cancellationToken);

        _logger.LogInformation("Step 4: Awaiting warehouse acknowledgement...");
        await Task.Delay(300, cancellationToken);

        _logger.LogInformation("Step 5: Warehouse notified. Dispatch ticket #WH-{Ticket} created.",
            Random.Shared.Next(10000, 99999));
        _logger.LogInformation("[{Thread}] NotifyWarehouseJob Completed", Thread.CurrentThread.Name);
    }
}