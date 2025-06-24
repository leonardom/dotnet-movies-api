using System;

namespace Movies.Api;

public static class ApiEndpoints
{
  private const string ApiBase = "api";

  public static class Movies
  {
    private const string ResourceBase = $"{ApiBase}/movies";

    public const string Create = ResourceBase;
    public const string Get = $"{ResourceBase}/{{idOrSlug}}";
    public const string GetAll = ResourceBase;
    public const string Update = $"{ResourceBase}/{{id:guid}}";
    public const string Delete = $"{ResourceBase}/{{id:guid}}";
    
    public const string Rate =  $"{ResourceBase}/{{id:guid}}/ratings";
    public const string DeleteRating = $"{ResourceBase}/{{id:guid}}/ratings";
  }

  public static class Ratings
  {
    private const string ResourceBase = $"{ApiBase}/ratings";
    
    public const string GetUserRatings = $"{ResourceBase}/me";
  }
}
