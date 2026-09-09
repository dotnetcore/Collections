using System;
using DotNetCore.Collections.Paginable;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.Tests
{
    public class PaginableSettingsTest
    {
        [Fact]
        public void DefaultSettingsShouldBeValid()
        {
            var settings = new PaginableSettings();
            settings.DefaultPageSize.ShouldBeGreaterThanOrEqualTo(1);
            settings.MaxMemberItems.ShouldBeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public void InvalidDefaultPageSizeShouldThrow()
        {
            var settings = new PaginableSettings();
            Should.Throw<ArgumentOutOfRangeException>(() => settings.DefaultPageSize = 0);
            Should.Throw<ArgumentOutOfRangeException>(() => settings.DefaultPageSize = -1);
        }

        [Fact]
        public void InvalidMaxMemberItemsShouldThrow()
        {
            var settings = new PaginableSettings();
            Should.Throw<ArgumentOutOfRangeException>(() => settings.MaxMemberItems = 0);
            Should.Throw<ArgumentOutOfRangeException>(() => settings.MaxMemberItems = -1);
        }

        [Fact]
        public void ValidSettingsShouldBeAccepted()
        {
            var settings = new PaginableSettings
            {
                DefaultPageSize = 50,
                MaxMemberItems = 1_000
            };
            settings.DefaultPageSize.ShouldBe(50);
            settings.MaxMemberItems.ShouldBe(1_000L);
        }

        [Fact]
        public void ManagerShouldReturnLatestSnapshot()
        {
            var original = PaginableSettingsManager.Settings;
            try
            {
                var snapshot = new PaginableSettings { DefaultPageSize = 20 };
                PaginableSettingsManager.UpdateSettings(snapshot);
                PaginableSettingsManager.Settings.DefaultPageSize.ShouldBe(20);
                PaginableSettingsManager.Settings.ShouldBeSameAs(snapshot);
            }
            finally
            {
                PaginableSettingsManager.UpdateSettings(original);
            }
        }
    }
}
