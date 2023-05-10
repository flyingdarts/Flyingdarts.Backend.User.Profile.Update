using Amazon.Lambda.APIGatewayEvents;
using MediatR;

public class UpdateUserProfileCommand : IRequest<APIGatewayProxyResponse>
{
    public string CognitoUserId {  get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Country { get; set; }
}