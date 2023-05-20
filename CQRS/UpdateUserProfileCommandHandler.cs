using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using Flyingdarts.Persistence;
using Flyingdarts.Shared;
using MediatR;
using Microsoft.Extensions.Options;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Flyingdarts.Lambdas.Shared;

public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, APIGatewayProxyResponse>
{
    private readonly IDynamoDBContext _dbContext;
    private readonly IOptions<ApplicationOptions> _options;

    public UpdateUserProfileCommandHandler(IDynamoDBContext DbContext, IOptions<ApplicationOptions> ApplicationOptions)
    {
        _dbContext = DbContext;
        _options = ApplicationOptions;
    }
    public async Task<APIGatewayProxyResponse> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var userProfile = UserProfile.Create(request.UserName, request.Email, request.Country);
        var user = User.Create(request.CognitoUserId, userProfile);

        var userWrite = _dbContext.CreateBatchWrite<User>(_options.Value.ToOperationConfig());
        userWrite.AddPutItem(user);

        await userWrite.ExecuteAsync(cancellationToken);
        var socketMessage = new SocketMessage<UpdateUserProfileCommand>();
        socketMessage.Message = request;
        socketMessage.Action = "v2/user/profile/update";

        var lambdaClient = new AmazonLambdaClient();
        var invokeRequest = new InvokeRequest()
        {
            FunctionName = "Flyingdarts-Backend-User-Profile-VerifyEmail",
            Payload = JsonSerializer.Serialize(new
            {
                Email = request.Email, 
                Subject = "UpdateUserProfileCommand", 
                Body = "Body from UpdateUserProfileVerifyEmail"
            })
        };

        await lambdaClient.InvokeAsync(invokeRequest);

        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(socketMessage)
        };
    }

    private static QueryOperationConfig QueryConfig(string cognitoUserId) 
    {
        var queryFilter = new QueryFilter("PK", QueryOperator.Equal, Constants.User);
        queryFilter.AddCondition("SK", QueryOperator.BeginsWith, cognitoUserId);
        return new QueryOperationConfig { Filter = queryFilter };
    }
}