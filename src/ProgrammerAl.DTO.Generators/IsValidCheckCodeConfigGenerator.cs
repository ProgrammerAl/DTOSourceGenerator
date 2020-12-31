﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

using ProgrammerAl.DTO.Attributes;

namespace ProgrammerAl.DTO.Generators
{
    public class IsValidCheckCodeConfigGenerator
    {
        public IDtoPropertyIsValidCheckConfig GeneratePropertyIsValidCheckRules(
            IPropertySymbol propertySymbol,
            AttributeSymbols attributeSymbols,
            ImmutableArray<string> allClassesAddingDto)
        {
            var propertyAttributes = propertySymbol.GetAttributes();

            if (DataTypeIsString(propertySymbol))
            {
                return CreateDtoPropertyCheckConfigForString(propertyAttributes, attributeSymbols);
            }
            else if (DataTypeIsAnotherDto(propertySymbol, allClassesAddingDto))
            {
                return CreateDtoPropertyCheckConfigForDto(propertyAttributes, attributeSymbols);
            }
            else if (DataTypeIsNullable(propertySymbol))
            {
                return new DtoBasicPropertyIsValidCheckConfig(AllowNull: true);
            }

            return CreateDefaultPropertyConfig();
        }

        private bool DataTypeIsString(IPropertySymbol propertySymbol)
        {
            string dataTypeFullName = PropertyUtilities.GenerateDataTypeFullNameFromProperty(propertySymbol);

            return
                string.Equals("string", dataTypeFullName, StringComparison.OrdinalIgnoreCase)
                || string.Equals("System.String", dataTypeFullName, StringComparison.OrdinalIgnoreCase);
        }

        private IDtoPropertyIsValidCheckConfig CreateDtoPropertyCheckConfigForString(ImmutableArray<AttributeData> propertyAttributes, AttributeSymbols attributeSymbols)
        {
            var stringPropertyAttribute = propertyAttributes.FirstOrDefault(x => x.AttributeClass?.Equals(attributeSymbols.DtoStringPropertyCheckAttributeSymbol, SymbolEqualityComparer.Default) is true);
            if (stringPropertyAttribute is object)
            {
                var value = stringPropertyAttribute.NamedArguments.SingleOrDefault(x => x.Key == nameof(StringPropertyCheckAttribute.StringIsValidCheckType)).Value;
                var stringCheckType = (StringIsValidCheckType)(value.Value!);

                return new DtoStringPropertyIsValidCheckConfig(stringCheckType);
            }

            //A string could instead have the DtoBasicPropertyCheckAttribute attribute applied. Check for that too
            DtoBasicPropertyIsValidCheckConfig? propertyCheckConfig = CreateBasicPropertyCheckFromAttribute(propertyAttributes, attributeSymbols);
            if (propertyCheckConfig is object && propertyCheckConfig.AllowNull)
            {
                return new DtoStringPropertyIsValidCheckConfig(StringIsValidCheckType.AllowNull);
            }

            return new DtoStringPropertyIsValidCheckConfig(StringPropertyCheckAttribute.DefaultStringIsValidCheckType);
        }

        private IDtoPropertyIsValidCheckConfig CreateDtoPropertyCheckConfigForBasicProperty(ImmutableArray<AttributeData> propertyAttributes, AttributeSymbols attributeSymbols)
        {
            DtoBasicPropertyIsValidCheckConfig? propertyCheckConfig = CreateBasicPropertyCheckFromAttribute(propertyAttributes, attributeSymbols);
            if (propertyCheckConfig is object)
            {
                return propertyCheckConfig;
            }

            return new DtoBasicPropertyIsValidCheckConfig(AllowNull: false);
        }


        private bool DataTypeIsAnotherDto(IPropertySymbol propertySymbol, ImmutableArray<string> allDtoNamesBeingGenerated)
        {
            string dataTypeFullName = PropertyUtilities.GenerateDataTypeFullNameFromProperty(propertySymbol);

            return allDtoNamesBeingGenerated.Any(x => string.Equals(x, dataTypeFullName, StringComparison.OrdinalIgnoreCase));
        }

        private bool DataTypeIsNullable(IPropertySymbol propertySymbol)
        {
            string dataTypeFullName = PropertyUtilities.GenerateDataTypeFullNameFromProperty(propertySymbol);

            //TODO: Check for Nullable<> too I guess
            return dataTypeFullName.EndsWith("?");
        }

        private DtoBasicPropertyIsValidCheckConfig? CreateBasicPropertyCheckFromAttribute(ImmutableArray<AttributeData> propertyAttributes, AttributeSymbols attributeSymbols)
        {
            var basicPropertyAttribute = propertyAttributes.FirstOrDefault(x => x.AttributeClass?.Equals(attributeSymbols.DtoBasicPropertyCheckAttributeSymbol, SymbolEqualityComparer.Default) is true);
            if (basicPropertyAttribute is object)
            {
                var allowNullValue = basicPropertyAttribute.NamedArguments.SingleOrDefault(x => x.Key == nameof(BasicPropertyCheckAttribute.AllowNull)).Value;
                var allowNull = (bool)allowNullValue.Value!;

                return new DtoBasicPropertyIsValidCheckConfig(allowNull);
            }

            return null;
        }

        private IDtoPropertyIsValidCheckConfig CreateDefaultPropertyConfig() => new DtoBasicPropertyIsValidCheckConfig(AllowNull: false);

        private IDtoPropertyIsValidCheckConfig CreateDtoPropertyCheckConfigForDto(ImmutableArray<AttributeData> propertyAttributes, AttributeSymbols attributeSymbols)
        {
            var dtoPropertyAttribute = propertyAttributes.FirstOrDefault(x => x.AttributeClass?.Equals(attributeSymbols.DtoPropertyCheckAttributeSymbol, SymbolEqualityComparer.Default) is true);
            if (dtoPropertyAttribute is object)
            {
                var checkIsValidValue = dtoPropertyAttribute.NamedArguments.SingleOrDefault(x => x.Key == nameof(DtoPropertyCheckAttribute.CheckIsValid)).Value;
                var checkIsValid = (bool)checkIsValidValue.Value!;

                var allowNullValue = dtoPropertyAttribute.NamedArguments.SingleOrDefault(x => x.Key == nameof(BasicPropertyCheckAttribute.AllowNull)).Value;
                var allowNull = (bool)allowNullValue.Value!;

                return new DtoPropertyIsValidCheckConfig(allowNull, checkIsValid);
            }

            DtoBasicPropertyIsValidCheckConfig? propertyCheckConfig = CreateBasicPropertyCheckFromAttribute(propertyAttributes, attributeSymbols);
            if (propertyCheckConfig is object)
            {
                return propertyCheckConfig;
            }

            return new DtoPropertyIsValidCheckConfig(CheckIsValid: DtoPropertyCheckAttribute.DefaultCheckIsValid, AllowNull: BasicPropertyCheckAttribute.DefaultAllowNull);
        }
    }
}
