using AasCore.Aas3_0;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Kernel;
using DataTransferObjects;
using DataTransferObjects.CommonDTOs;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.Exceptions;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;
using Moq;
using File = AasCore.Aas3_0.File;

namespace IO.Swagger.Lib.V3.Tests.SerializationModifiers.Mappers.ValueMappers;

public class ResponseValueTransformerTests
{
    private readonly Fixture _fixture;
    private readonly ResponseValueTransformer _transformer;

    public ResponseValueTransformerTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        _transformer = new ResponseValueTransformer();
    }

    #region Transform

    [Fact]
    public void Transform_WithNullClass_ShouldReturnNull()
    {
        // Arrange
        // Act
        var result = _transformer.Transform(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Transform_WithIClassObject_ShouldTransformToIDTO()
    {
        // Arrange
        var classObject = _fixture.Create<IClass>();

        // Act
        var result = _transformer.Transform(classObject);

        // Assert
        result.Should().NotBeNull();
        result.GetType().ToString().Should().Be("Castle.Proxies.IDTOProxy", "whatevert happens here that IDTO is not the real type");
    }

    #endregion

    #region TransformAnnotatedRelationshipElement

    [Fact]
    public void TransformAnnotatedRelationshipElement_ReturnsNull_WhenParameterIsNull()
    {
        // Arrange
        // Act
        var result = _transformer.TransformAnnotatedRelationshipElement(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TransformAnnotatedRelationshipElement_ReturnsNull_WhenParameterAnnotationsIsNull()
    {
        // Arrange
        var annotatedElement = _fixture.Create<IAnnotatedRelationshipElement>();
        annotatedElement.Annotations = null;

        // Act
        var result = _transformer.TransformAnnotatedRelationshipElement(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Skip = "not testable at the moment because of the current dependencies between classes")]
    public void TransformAnnotatedRelationshipElement_ReturnsDTO_WhenAnnotationsAreNotNull()
    {
        // Arrange
        var annotatedElement = _fixture.Create<IAnnotatedRelationshipElement>();
        var annotations = _fixture.CreateMany<IDataElement>().ToList();
        annotatedElement.Annotations = annotations;

        // Act
        var result = _transformer.TransformAnnotatedRelationshipElement(annotatedElement);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<AnnotatedRelationshipElementValue>();
        var annotatedResult = (AnnotatedRelationshipElementValue) result;
        annotatedResult.idShort.Should().Be(annotatedElement.IdShort ?? string.Empty);
        annotatedResult.first.Should().BeEquivalentTo(_transformer.Transform(annotatedElement.First));
        annotatedResult.second.Should().BeEquivalentTo(_transformer.Transform(annotatedElement.Second));
        annotatedResult.annotations.Should().BeEquivalentTo(annotations);
    }

    #endregion

    #region TransformCapability

    [Fact]
    public void TransformCapability_ThrowsException_WhenCapabilityIsNotNull()
    {
        // Arrange
        var capability = new Mock<ICapability>().Object;

        // Act
        Action act = () => _transformer.TransformCapability(capability);

        // Assert
        act.Should().Throw<InvalidSerializationModifierException>()
            .WithMessage("Invalid serialization modifier ValueOnly for the requested element of type ICapabilityProxy."); // Adjust the expected message as needed
    }

    [Fact]
    public void TransformCapability_ThrowsException_WhenCapabilityIsNull()
    {
        // Arrange
        var capability = _fixture.Create<ICapability>();

        // Act
        Action act = () => _transformer.TransformCapability(capability);

        // Assert
        act.Should().Throw<InvalidSerializationModifierException>()
            .WithMessage("Invalid serialization modifier ValueOnly for the requested element of type ICapabilityProxy."); // Adjust the expected message as needed
    }

    #endregion

    #region TransformEntity

    [Fact(Skip = "Can't figure it out")]
    public void TransformEntity_ReturnsEntityValue_WhenStatementsNotNull()
    {
        // Arrange
        var statements = new List<ISubmodelElement> {_fixture.Create<IOperation>()}; // Sample valid statements
        var entity = _fixture.Create<Entity>();
        // Act
        var result = _transformer.TransformEntity(entity);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<EntityValue>();
        var entityValue = (EntityValue) result;
        entityValue.statements.Should().NotBeNull();
        entityValue.statements!.Count.Should().Be(statements.Count);
    }

    [Fact]
    public void TransformEntity_ReturnsEntityValue_WhenStatementsAreNull()
    {
        // Arrange
        var entity = new Mock<IEntity>();
        entity.Setup(e => e.Statements).Returns((List<ISubmodelElement>?) null);
        entity.Setup(e => e.IdShort).Returns("entityId");

        // Act
        var result = _transformer.TransformEntity(entity.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<EntityValue>();
        var entityValue = (EntityValue) result;
        entityValue.statements.Should().BeNull();
    }

    #endregion

    #region TransformFile

    [Fact]
    public void TransformFile_ReturnsFileValue_WhenFileIsNotNull()
    {
        // Arrange
        const string fileIdShort = "fileId";
        const string contentType = "application/pdf";
        var file = _fixture.Build<File>()
            .With(x => x.IdShort, fileIdShort)
            .With(x => x.ContentType, contentType)
            .Create();

        // Act
        var result = _transformer.TransformFile(file);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<FileValue>();
        var fileResult = (FileValue) result;
        fileResult.idShort.Should().Be(fileIdShort);
        fileResult.contentType.Should().Be(contentType);
    }

    #endregion

    #region TransformKey

    [Fact]
    public void TransformKey_ReturnsKeyDTO_WhenKeyIsNotNull()
    {
        // Arrange
        const KeyTypes keyType = KeyTypes.File;
        const string keyValue = "KeyValue";
        var keyMock = new Mock<IKey>();
        keyMock.Setup(k => k.Type).Returns(keyType);
        keyMock.Setup(k => k.Value).Returns(keyValue);

        // Act
        var result = _transformer.TransformKey(keyMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<KeyDTO>();
        var keyResult = (KeyDTO) result;
        keyResult.type.Should().Be(keyType);
        keyResult.value.Should().Be(keyValue);
    }

    #endregion

    #region TransformMultiLanguageProperty

    [Fact]
    public void TransformMultiLanguageProperty_ReturnsMultiLanguagePropertyValue_WhenMultiLanguagePropertyIsNotNull()
    {
        // Arrange
        // Arrange
        const string idShort = "multiLanguageId";
        var langString1 = new LangStringTextType("en", "English Text");
        var langString2 = new LangStringTextType("fr", "French Text");
        var multiLanguagePropertyMock = new Mock<IMultiLanguageProperty>();
        multiLanguagePropertyMock.Setup(m => m.IdShort).Returns(idShort);
        multiLanguagePropertyMock.Setup(m => m.Value).Returns(new List<ILangStringTextType> {langString1, langString2});

        // Act
        var result = _transformer.TransformMultiLanguageProperty(multiLanguagePropertyMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<MultiLanguagePropertyValue>();
        var multiLanguageResult = (MultiLanguagePropertyValue) result;
        multiLanguageResult.idShort.Should().Be(idShort);
        multiLanguageResult.langStrings.Should().HaveCount(2);
        multiLanguageResult.langStrings.Should().Contain(new KeyValuePair<string, string>(langString1.Language, langString1.Text));
        multiLanguageResult.langStrings.Should().Contain(new KeyValuePair<string, string>(langString2.Language, langString2.Text));
    }

    [Fact]
    public void TransformMultiLanguageProperty_ReturnsMultiLanguagePropertyValueWithEmptyLangStrings_WhenMultiLanguagePropertyValueHasNullValue()
    {
        // Arrange
        const string idShort = "multiLanguageId";
        var multiLanguagePropertyMock = new Mock<IMultiLanguageProperty>();
        multiLanguagePropertyMock.Setup(m => m.IdShort).Returns(idShort);
        multiLanguagePropertyMock.Setup(m => m.Value).Returns((List<ILangStringTextType>) null);

        // Act
        var result = _transformer.TransformMultiLanguageProperty(multiLanguagePropertyMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<MultiLanguagePropertyValue>();
        var multiLanguageResult = (MultiLanguagePropertyValue) result;
        multiLanguageResult.idShort.Should().Be(idShort);
        multiLanguageResult.langStrings.Should().BeEmpty();
    }

    #endregion

    #region TransformOperation

    [Fact]
    public void TransformOperation_ReturnsOperationValue_WhenOperationIsNotNull()
    {
        // Arrange
        const string idShort = "operationId";
        var inputVariables = new List<IOperationVariable>();
        var outputVariables = new List<IOperationVariable>();
        var inoutputVariables = new List<IOperationVariable>();

        // Populate inputVariables, outputVariables, and inoutputVariables with mock objects as needed

        var operationMock = new Mock<IOperation>();
        operationMock.Setup(o => o.IdShort).Returns(idShort);
        operationMock.Setup(o => o.InputVariables).Returns(inputVariables);
        operationMock.Setup(o => o.OutputVariables).Returns(outputVariables);
        operationMock.Setup(o => o.InoutputVariables).Returns(inoutputVariables);

        // Act
        var result = _transformer.TransformOperation(operationMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<OperationValue>();
        var operationResult = (OperationValue) result;
        operationResult.idShort.Should().Be(idShort);
    }

    [Fact]
    public void TransformOperation_ReturnsOperationValueWithEmptyLists_WhenOperationHasNullVariables()
    {
        // Arrange
        const string idShort = "operationId";
        List<IOperationVariable> inputVariables = null;
        List<IOperationVariable> outputVariables = null;
        List<IOperationVariable> inoutputVariables = null;

        var operationMock = new Mock<IOperation>();
        operationMock.Setup(o => o.IdShort).Returns(idShort);
        operationMock.Setup(o => o.InputVariables).Returns(inputVariables);
        operationMock.Setup(o => o.OutputVariables).Returns(outputVariables);
        operationMock.Setup(o => o.InoutputVariables).Returns(inoutputVariables);

        // Act
        var result = _transformer.TransformOperation(operationMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<OperationValue>();
        var operationResult = (OperationValue) result;
        operationResult.idShort.Should().Be(idShort);
        operationResult.inputVariables.Should().BeNull();
        operationResult.outputVariables.Should().BeNull();
        operationResult.inoutputvariables.Should().BeNull();
    }

    #endregion

    #region TransformOperationVariable

    [Fact]
    public void TransformOperationVariable_ReturnsTransformedValue_WhenOperationVariableIsNotNull()
    {
        // Arrange
        var operationVariableMock = _fixture.Create<OperationVariable>();

        // Act
        var result = _transformer.TransformOperationVariable(operationVariableMock);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region TransformSubmodelElementCollection

    [Fact]
    public void TransformSubmodelElementCollection_ReturnsSubmodelElementCollectionValueWithEmptyList_WhenValueIsNull()
    {
        // Arrange
        const string idShort = "collectionId";
        var submodelElementCollectionMock = new Mock<ISubmodelElementCollection>();
        submodelElementCollectionMock.Setup(m => m.IdShort).Returns(idShort);
        submodelElementCollectionMock.Setup(m => m.Value).Returns((Func<List<ISubmodelElement>?>) null);

        // Act
        var result = _transformer.TransformSubmodelElementCollection(submodelElementCollectionMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SubmodelElementCollectionValue>();
        var collectionResult = (SubmodelElementCollectionValue) result;
        collectionResult.idShort.Should().Be(idShort);
        collectionResult.value.Should().BeNull();
    }

    #endregion

    #region TransformSubmodelElementList

    [Fact (Skip = "cant figure it out")]
    public void TransformSubmodelElementList_ReturnsSubmodelElementListValue_WhenValueIsNotNull()
    {
        // Arrange
        const string idShort = "listId";
        var submodelElementValues = _fixture.CreateMany<ISubmodelElementValue>().ToList();
        var submodelElementListMock = new Mock<ISubmodelElementList>();
        submodelElementListMock.Setup(m => m.IdShort).Returns(idShort);

        // Act
        var result = _transformer.TransformSubmodelElementList(submodelElementListMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SubmodelElementListValue>();
        var listResult = (SubmodelElementListValue) result;
        listResult.idShort.Should().Be(idShort);
        listResult.value.Should().BeEquivalentTo(submodelElementValues);
    }

    [Fact]
    public void TransformSubmodelElementList_ReturnsSubmodelElementListValueWithEmptyList_WhenValueIsNull()
    {
        // Arrange
        const string idShort = "listId";
        var submodelElementListMock = new Mock<ISubmodelElementList>();
        submodelElementListMock.Setup(m => m.IdShort).Returns(idShort);

        // Act
        var result = _transformer.TransformSubmodelElementList(submodelElementListMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SubmodelElementListValue>();
        var listResult = (SubmodelElementListValue) result;
        listResult.idShort.Should().Be(idShort);
        listResult.value.Should().BeNull();
    }

    #endregion


    #region not implemented methods

    [Fact]
    public void TransformAdministrativeInformation_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformAdministrativeInformation(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformAssetAdministrationShell_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformAssetAdministrationShell(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformAssetInformation_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformAssetInformation(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }


    [Fact]
    public void TransformCapability_ShouldThrow_InvalidSerializationModifierException()
    {
        // Arrange
        _fixture.Customizations.Add(
            new TypeRelay(
                typeof(ICapability),
                typeof(Capability)));

        var capability = _fixture.Create<Capability>();

        // Act
        Action act = () => _transformer.TransformCapability(capability);

        // Assert
        act.Should().Throw<InvalidSerializationModifierException>();
    }


    [Fact]
    public void TransformConceptDescription_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformConceptDescription(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformDataSpecificationIec61360_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformDataSpecificationIec61360(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformEmbeddedDataSpecification_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformEmbeddedDataSpecification(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformEnvironment_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformEnvironment(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformEventPayload_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformEventPayload(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformExtension_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformExtension(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformLangStringDefinitionTypeIec61360_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformLangStringDefinitionTypeIec61360(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformLangStringNameType_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformLangStringNameType(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformLangStringPreferredNameTypeIec61360_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformLangStringPreferredNameTypeIec61360(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformLangStringShortNameTypeIec61360_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformLangStringShortNameTypeIec61360(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformLangStringTextType_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformLangStringTextType(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformLevelType_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformLevelType(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformQualifier_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformQualifier(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformResource_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformResource(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformSpecificAssetId_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformSpecificAssetId(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformValueList_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformValueList(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformValueReferencePair_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformValueReferencePair(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    #endregion
}