namespace SalesAR.UseCases.Payments;

using SalesAR.CoreBusiness.Models;

public interface IProcessPaymentUseCase
{
    Task<int> ExecuteAsync(Payment payment);
}

public class ProcessPaymentUseCase : IProcessPaymentUseCase
{
    private readonly IRecordPaymentUseCase _recordPaymentUseCase;

    public ProcessPaymentUseCase(IRecordPaymentUseCase recordPaymentUseCase)
    {
        _recordPaymentUseCase = recordPaymentUseCase;
    }

    public async Task<int> ExecuteAsync(Payment payment)
    {
        return await _recordPaymentUseCase.ExecuteAsync(payment);
    }
}

