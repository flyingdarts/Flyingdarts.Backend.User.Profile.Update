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
using System.Linq;
using System.Diagnostics;
using System.Net.Http;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;

public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, APIGatewayProxyResponse>
{
    private readonly IDynamoDBContext _dbContext;
    private readonly ApplicationOptions _options;

    public UpdateUserProfileCommandHandler(IDynamoDBContext DbContext, IOptions<ApplicationOptions> ApplicationOptions)
    {
        _dbContext = DbContext;
        _options = ApplicationOptions.Value;
    }
    public async Task<APIGatewayProxyResponse> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var userProfile = UserProfile.Create(request.UserName, request.Email, request.Country);
        var user = User.Create(request.CognitoUserId, userProfile);

        var updateRequest = new UpdateItemRequest
        {
            TableName = _options.DynamoDbTable,
            Key = new Dictionary<string, AttributeValue>
        {
            { "UserId", new AttributeValue { S = request.CognitoUserId } }
        },
            ExpressionAttributeNames = new Dictionary<string, string>
        {
            { "#profile", "Profile" }
        },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":profileValue", new AttributeValue { S = JsonSerializer.Serialize(userProfile) } }
        },
            UpdateExpression = "SET #profile = :profileValue",
            ReturnValues = ReturnValue.ALL_NEW
        };

        _dbContext.

        var response = await _dbContext.UpdateItemAsync(updateRequest, cancellationToken);

        var updatedProfile = JsonSerializer.Deserialize<UserProfile>(response.Attributes["Profile"].S);

        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(updatedProfile)
        };
    }

    private static QueryOperationConfig QueryConfig(string cognitoUserId) 
    {
        var queryFilter = new QueryFilter("PK", QueryOperator.Equal, Constants.User);
        queryFilter.AddCondition("SK", QueryOperator.BeginsWith, cognitoUserId);
        return new QueryOperationConfig { Filter = queryFilter };
    }
}