using System;
using System.Collections.Generic;

namespace RockSweeper.HereRestApi
{
    public class MetaInfo
    {
        public DateTime Timestamp { get; set; }
        public string NextPageInformation { get; set; }
    }

    public class LocationMatchQuality
    {
        public double Country { get; set; }
        public double State { get; set; }
        public double County { get; set; }
        public double City { get; set; }
        public double District { get; set; }
        public List<double> Street { get; set; }
        public double HouseNumber { get; set; }
        public double PostalCode { get; set; }
    }

    public class DisplayPosition
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class NavigationPosition
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class TopLeft
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class BottomRight
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class MapView
    {
        public TopLeft TopLeft { get; set; }
        public BottomRight BottomRight { get; set; }
    }

    public class AdditionalData
    {
        public string value { get; set; }
        public string key { get; set; }
    }

    public class Address
    {
        public string Label { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string County { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string Street { get; set; }
        public string HouseNumber { get; set; }
        public string PostalCode { get; set; }
        public List<AdditionalData> AdditionalData { get; set; }
    }

    public class MapReference
    {
        public string ReferenceId { get; set; }
        public string MapId { get; set; }
        public string MapVersion { get; set; }
        public string MapReleaseDate { get; set; }
        public double Spot { get; set; }
        public string SideOfStreet { get; set; }
        public string CountryId { get; set; }
        public string StateId { get; set; }
        public string CountyId { get; set; }
        public string CityId { get; set; }
        public string BuildingId { get; set; }
        public string AddressId { get; set; }
    }

    public class Location
    {
        public string LocationId { get; set; }
        public string LocationType { get; set; }
        public DisplayPosition DisplayPosition { get; set; }
        public List<NavigationPosition> NavigationPosition { get; set; }
        public MapView MapView { get; set; }
        public Address Address { get; set; }
        public MapReference MapReference { get; set; }
    }

    public class LocationResult
    {
        public double Relevance { get; set; }
        public double Distance { get; set; }
        public string MatchLevel { get; set; }
        public LocationMatchQuality MatchQuality { get; set; }
        public string MatchType { get; set; }
        public Location Location { get; set; }
    }

    public class View<T>
    {
        public string _type { get; set; }
        public int ViewId { get; set; }
        public List<T> Result { get; set; }
    }

    public class Response<T>
    {
        public MetaInfo MetaInfo { get; set; }
        public List<View<T>> View { get; set; }
    }

    public class ApiResponse<T>
    {
        public Response<T> Response { get; set; }
    }
}
    