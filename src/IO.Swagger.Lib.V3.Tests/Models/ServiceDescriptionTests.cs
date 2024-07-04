namespace IO.Swagger.Lib.V3.Tests.Models;

using JetBrains.Annotations;
using Swagger.Models;

[TestSubject(typeof(ServiceDescription))]
public class ServiceDescriptionTests
{
     [Fact]
        public void ToString_ShouldReturnFormattedString()
        {
            // Arrange
            var serviceDescription = new ServiceDescription
            {
                Profiles = new List<ServiceDescription.ServiceProfiles>
                {
                    ServiceDescription.ServiceProfiles.DiscoveryServiceSpecificationSSP001
                }
            };

            // Act
            var result = serviceDescription.ToString();

            // Assert
            result.Should().Contain("class ServiceDescription {");
            result.Should().Contain("Profiles:");
        }

        [Fact]
        public void ToJson_ShouldReturnIndentedJsonString()
        {
            // Arrange
            var serviceDescription = new ServiceDescription
            {
                Profiles = new List<ServiceDescription.ServiceProfiles>
                {
                    ServiceDescription.ServiceProfiles.DiscoveryServiceSpecificationSSP001
                }
            };

            // Act
            var result = serviceDescription.ToJson();

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("\"Profiles\":");
            result.Should().Contain("\"DiscoveryServiceSpecificationSSP001\"");
        }

        [Fact]
        public void Equals_WithSameObject_ShouldReturnTrue()
        {
            // Arrange
            var serviceDescription = new ServiceDescription
            {
                Profiles = new List<ServiceDescription.ServiceProfiles>
                {
                    ServiceDescription.ServiceProfiles.DiscoveryServiceSpecificationSSP001
                }
            };

            // Act
            var result = serviceDescription.Equals(serviceDescription);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_WithEqualObject_ShouldReturnTrue()
        {
            // Arrange
            var profiles = new List<ServiceDescription.ServiceProfiles>
            {
                ServiceDescription.ServiceProfiles.DiscoveryServiceSpecificationSSP001
            };

            var serviceDescription1 = new ServiceDescription { Profiles = profiles };
            var serviceDescription2 = new ServiceDescription { Profiles = profiles };

            // Act
            var result = serviceDescription1.Equals(serviceDescription2);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_WithDifferentObject_ShouldReturnFalse()
        {
            // Arrange
            var serviceDescription1 = new ServiceDescription
            {
                Profiles = new List<ServiceDescription.ServiceProfiles>
                {
                    ServiceDescription.ServiceProfiles.DiscoveryServiceSpecificationSSP001
                }
            };

            var serviceDescription2 = new ServiceDescription
            {
                Profiles = new List<ServiceDescription.ServiceProfiles>
                {
                    ServiceDescription.ServiceProfiles.AssetAdministrationShellRepositoryServiceSpecificationSSP001
                }
            };

            // Act
            var result = serviceDescription1.Equals(serviceDescription2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_ShouldReturnCorrectHashCode()
        {
            // Arrange
            var profiles = new List<ServiceDescription.ServiceProfiles>
            {
                ServiceDescription.ServiceProfiles.DiscoveryServiceSpecificationSSP001
            };

            var serviceDescription = new ServiceDescription { Profiles = profiles };
            var expectedHashCode = 41;
            expectedHashCode = (expectedHashCode * 59) + profiles.GetHashCode();

            // Act
            var result = serviceDescription.GetHashCode();

            // Assert
            result.Should().Be(expectedHashCode);
        }

        [Fact]
        public void OperatorEquals_WithEqualObjects_ShouldReturnTrue()
        {
            // Arrange
            var profiles = new List<ServiceDescription.ServiceProfiles>
            {
                ServiceDescription.ServiceProfiles.DiscoveryServiceSpecificationSSP001
            };

            var serviceDescription1 = new ServiceDescription { Profiles = profiles };
            var serviceDescription2 = new ServiceDescription { Profiles = profiles };

            // Act
            var result = serviceDescription1 == serviceDescription2;

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void OperatorNotEquals_WithDifferentObjects_ShouldReturnTrue()
        {
            // Arrange
            var serviceDescription1 = new ServiceDescription
            {
                Profiles = new List<ServiceDescription.ServiceProfiles>
                {
                    ServiceDescription.ServiceProfiles.DiscoveryServiceSpecificationSSP001
                }
            };

            var serviceDescription2 = new ServiceDescription
            {
                Profiles = new List<ServiceDescription.ServiceProfiles>
                {
                    ServiceDescription.ServiceProfiles.SubmodelServiceSpecificationSSP002
                }
            };

            // Act
            var result = serviceDescription1 != serviceDescription2;

            // Assert
            result.Should().BeTrue();
        }
}