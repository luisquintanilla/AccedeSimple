namespace AccedeSimple.Domain;
public record Attraction(
    string Name,
    string Description,
    string Address,
    float Rating,
    string OperatingHours
);

public record CityAttractions(
    string City,
    List<Attraction> Attractions
);