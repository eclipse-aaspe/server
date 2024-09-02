/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

namespace AasxCsharpLibary.Tests.AasxCompatibilityModels.V20;

using System.Text;
using System.Text.Json;
using AdminShell_V20;
using global::AasxCompatibilityModels;

public class AdminShellConvertersTests
{
    private readonly Fixture _fixture;

    public AdminShellConvertersTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void JsonAasxConverter_CanConvert_Should_Return_True_For_Referable()
    {
        // Arrange
        var converter     = new AdminShell_V20.AdminShellConverters.JsonAasxConverter();
        var typeToConvert = typeof(AdminShellV20.Referable);

        // Act
        var result = converter.CanConvert(typeToConvert);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void JsonAasxConverter_Read_Should_Populate_Target_Object()
    {
        // Arrange
        var jsonString = "{\"modelType\": { \"name\": \"SubmodelElement\" }, \"otherProperty\": \"value\" }";
        var options    = new JsonSerializerOptions(); // Use real JsonSerializerOptions
        var bytes      = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var reader     = new Utf8JsonReader(bytes, new JsonReaderOptions {AllowTrailingCommas = true});

        var converter = new AdminShell_V20.AdminShellConverters.JsonAasxConverter();

        // Act
        var result = converter.Read(ref reader, typeof(AdminShellV20.Referable), options);

        // Assert
        result.Should().BeOfType<AdminShellV20.Referable>(); // Adjust type as needed based on the actual return type

        result.Should().NotBeNull();
        result.category.Should().BeNull();
        result.description.Should().BeNull();
        result.idShort.Should().Be(string.Empty);
        result.parent.Should().BeNull();
        result.DiaryData.Should().NotBeNull();
        result.DiaryData.Entries.Should().BeNull();
        result.DiaryData.TimeStamp.Should().NotBeNull();
        result.JsonDescription.Should().BeNull();
    }

    [Fact]
    public void JsonAasxConverter_Read_Should_Handle_JsonDescription()
    {
        // Arrange
        const string jsonString = @"
        {
            ""idShort"": ""123asd"",
            ""category"": ""test"",
            ""description"": {
                ""langString"": [
                    {
                        ""lang"": ""en"",
                        ""str"": ""Description text""
                    }
                ]
            }
        }";

        var options = new JsonSerializerOptions(); // Use real JsonSerializerOptions
        var bytes   = Encoding.UTF8.GetBytes(jsonString);
        var reader  = new Utf8JsonReader(bytes, new JsonReaderOptions { AllowTrailingCommas = true });

        var converter = new AdminShellConverters.JsonAasxConverter();

        // Act
        var result = converter.Read(ref reader, typeof(AdminShellV20.Referable), options) as AdminShellV20.Referable;

        // Assert
        result.Should().NotBeNull();
        result.JsonDescription.Should().NotBeNull();
        result.JsonDescription.Count.Should().Be(1);

        var firstLangStr = result.JsonDescription.FirstOrDefault();
        firstLangStr.Should().NotBeNull();
        firstLangStr.lang.Should().Be("en");
        firstLangStr.str.Should().Be("Description text");
    }
    
    [Fact]
    public void Referable_Validate_Should_Add_Error_For_Missing_IdShort()
    {
        // Arrange
        var referable = _fixture.Create<AdminShellV20.Referable>();
        referable.idShort = null; // Simulate missing idShort

        var validationRecords = new AasValidationRecordList();

        // Act
        referable.Validate(validationRecords);

        // Assert
        validationRecords.Should().ContainSingle(record =>
                                                     record.Severity == AasValidationSeverity.SpecViolation &&
                                                     record.Message == "Referable: missing idShort");
    }

    [Fact]
    public void Referable_ComputeHashcode_Should_Return_Correct_Hash()
    {
        // Arrange
        var referable = new AdminShellV20.Referable("testId");

        // Act
        var hashcode = referable.ComputeHashcode();

        // Assert
        hashcode.Should().NotBeNullOrEmpty();
        hashcode.Length.Should().Be(64); // SHA256 hash length
    }

    [Fact]
    public void Referable_CollectIdShortByParent_Should_Return_Correct_Path()
    {
        // Arrange
        var parent = _fixture.Create<AdminShellV20.Referable>();
        parent.idShort = "parent";

        var child = _fixture.Create<AdminShellV20.Referable>();
        child.idShort = "child";
        child.parent  = parent;

        // Act
        var fullPath = child.CollectIdShortByParent();

        // Assert
        fullPath.Should().Be("parent/child");
    }


    [Fact]
    public void JsonAasxConverter_Write_Should_ThrowNotImplementedException()
    {
        // Arrange
        var originalReferable = _fixture.Create<AdminShellV20.Referable>();
        var converter         = new AdminShell_V20.AdminShellConverters.JsonAasxConverter();
        var options           = new JsonSerializerOptions {WriteIndented = true};
        var buffer            = new MemoryStream();
        var writer            = new Utf8JsonWriter(buffer);

        // Act
        Action act = () => converter.Write(writer, originalReferable, options);

        // Assert
        act.Should().ThrowExactly<NotImplementedException>("we did not implement that yet and it is not needed yet");
    }
}