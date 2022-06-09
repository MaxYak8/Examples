// startup

services.AddConfiguredAuthorization();

// AuthorizationServiceExtension

public static IServiceCollection AddConfiguredAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(auth =>
            {
                auth.AddPolicy(Policies.ShouldBeAdmin, policy => policy.RequireRole(Roles.Admin));
                
				});

            return services;
        }



// controller method attribute
[CustomAuthorize(Policies.ShouldBeAdmin)]

// CustomAuthorizeAttribute

public class CustomAuthorizeAttribute : TypeFilterAttribute
    {
        public CustomAuthorizeAttribute() : base(typeof(CustomAuthorizationFilter))
        {
            Arguments = new object[] { string.Empty };
        }

        public CustomAuthorizeAttribute(string policy) : base(typeof(CustomAuthorizationFilter))
        {
            Arguments = new object[] { policy };
        }
    }
	
// CustomAuthorizationFilter

public class CustomAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly IAuthorizationService authorizationService;
        private readonly IJwtService tokenService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly string policy;

        public CustomAuthorizationFilter(
            string policy,
            IAuthorizationService authorizationService,
            IJwtService tokenService,
            IHttpContextAccessor httpContextAccessor)
        {
            this.policy = policy;
            this.authorizationService = authorizationService;
            this.tokenService = tokenService;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context.HttpContext.User == null || context.HttpContext.User.Identity == null || !context.HttpContext.User.Identity.IsAuthenticated)
            {
                throw new AppException(LocalizationDictionary.GetResourceValue(nameof(ApiResponseResources.Your_session_has_expired__please_sign_in_again_)), null, ApplicationErrorKeys.TokenNotValidOrExpired, HttpStatusCode.Unauthorized);
            }

            var authorizationHeader = httpContextAccessor
                .HttpContext.Request.Headers["authorization"];

            var token = string.IsNullOrEmpty(authorizationHeader)
                ? string.Empty
                : authorizationHeader.Single().Split(" ").Last();

            if (!await tokenService.IsActiveAsync(token))
            {
                throw new AppException(LocalizationDictionary.GetResourceValue(nameof(ApiResponseResources.Your_session_has_expired__please_sign_in_again_)), null, ApplicationErrorKeys.TokenNotValidOrExpired, HttpStatusCode.Unauthorized);
            }

            if (!string.IsNullOrWhiteSpace(policy))
            {
                var authorized = await authorizationService.AuthorizeAsync(context.HttpContext.User, policy);

                if (!authorized.Succeeded)
                {
                    var errorsContract = new ErrorsContract
                    {
                        Errors = new List<ErrorContract>
                        {
                            new ErrorContract
                            {
                                ErrorKey = ApplicationErrorKeys.NoAccessForCurrentRole
                            }
                        }
                    };

                    var serializerSettings = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var result = new ContentResult
                    {
                        StatusCode = (int)HttpStatusCode.Forbidden,
                        Content = JsonSerializer.Serialize(errorsContract, serializerSettings),
                        ContentType = "application/json"
                    };

                    context.Result = result;
                }
            }
        }
    }
	
