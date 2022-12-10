// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Demo.Data;

namespace Demo.Services;

public interface IWeatherForecastService
{
    Task<WeatherForecast[]> GetForecastAsync(DateOnly startDate);
}
