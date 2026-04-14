using MediatR;

public class UpdatePOStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string Status { get; set; }

    public UpdatePOStatusCommand(Guid id, string status)
    {
        Id = id;
        Status = status;
    }
}
