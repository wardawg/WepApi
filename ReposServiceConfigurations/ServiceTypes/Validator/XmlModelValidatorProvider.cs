﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ReposServiceConfigure.ServiceTypes.Validators
{
    public class XmlModelValidatorProvider : ModelValidatorProvider
    {
        // Dictionary to temporarily store all the validation attribute types present in System.ComponentModel.DataAnnotations assembly.
        public readonly Dictionary<string, Type> _validatorTypes;

        public string XmlFolderPath = HttpContext.Current.Server.MapPath("Rules");
        
        public XmlModelValidatorProvider()
        {

          
            _validatorTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(t => t.FullName.Contains("DataAnnotations"))
            .First().GetTypes().Where(w => w.IsSubclassOf(typeof(ValidationAttribute)))
            .ToDictionary(t => t.Name, t => t);


            // custom ValidationAttribute that validates a date for future value.
            //  _validatorTypes.Add("FutureAttribute", typeof(FutureAttribute));
        }

        #region Stolen from DataAnnotationsModelValidatorProvider

        // delegate that converts ValidationAttribute into DataAnnotationsModelValidator
        internal static DataAnnotationsModelValidationFactory DefaultAttributeFactory =
    (metadata, context, attribute) => new DataAnnotationsModelValidator(metadata, context, attribute);

        internal static Dictionary<Type, DataAnnotationsModelValidationFactory> AttributeFactories = new Dictionary<Type, DataAnnotationsModelValidationFactory>()
{
{
typeof(RangeAttribute),
(metadata, context, attribute) => new RangeAttributeAdapter(metadata, context, (RangeAttribute)attribute)
},
{
typeof(RegularExpressionAttribute),
(metadata, context, attribute) => new RegularExpressionAttributeAdapter(metadata, context, (RegularExpressionAttribute)attribute)
},
{
typeof(RequiredAttribute),
(metadata, context, attribute) => new RequiredAttributeAdapter(metadata, context, (RequiredAttribute)attribute)
},
{
typeof(StringLengthAttribute),
(metadata, context, attribute) => new StringLengthAttributeAdapter(metadata, context, (StringLengthAttribute)attribute)
},
};

        #endregion

        public override IEnumerable<ModelValidator> GetValidators(ModelMetadata metadata, ControllerContext context)
        {
            var results = new List<ModelValidator>();

            var v = new SubCategoryModelValidator();

            // whether the validation is for a property or model
            // (remember we can apply validation attributes to a property or model and same applies here as well)
            var isPropertyValidation = metadata.ContainerType != null && !String.IsNullOrEmpty(metadata.PropertyName);

            var rulesPath = String.Format("{0}\\{1}.xml", XmlFolderPath,
            isPropertyValidation ? metadata.ContainerType.Name : metadata.ModelType.Name);

            var rules = File.Exists(rulesPath) ? XElement.Load(rulesPath).XPathSelectElements(String.Format("./validator[@property='{0}']",
            isPropertyValidation ? metadata.PropertyName : metadata.ModelType.Name)).ToList() : new List<XElement>();

            // Produce a validator for each validation attribute we find
            foreach (var rule in rules)
            {
                DataAnnotationsModelValidationFactory factory;

                var validatorType = _validatorTypes[String.Concat(rule.Attribute("type").Value, "Attribute")];

                if (!AttributeFactories.TryGetValue(validatorType, out factory))
                {
                    factory = DefaultAttributeFactory;
                }

                var validator = (ValidationAttribute)Activator.CreateInstance(validatorType, GetValidationArgs(rule));
                validator.ErrorMessage = rule.Attribute("message") != null && !String.IsNullOrEmpty(rule.Attribute("message").Value) ? rule.Attribute("message").Value : null;
                results.Add(factory(metadata, context, validator));
            }

            return results;
        }

        // read the arguments passed to the validation attribute and cast it their respective type.
        private object[] GetValidationArgs(XElement rule)
        {
            var validatorArgs = rule.Attributes().Where(a => a.Name.ToString().StartsWith("arg"));
            var args = new object[validatorArgs.Count()];
            var i = 0;

            foreach (var arg in validatorArgs)
            {
                var argName = arg.Name.ToString();
                var argValue = arg.Value;

                if (!argName.Contains("-"))
                {
                    args[i] = argValue;
                }
                else
                {
                    var argType = argName.Split('-')[1];

                    switch (argType)
                    {
                        case "int":
                            args[i] = int.Parse(argValue);
                            break;

                        case "datetime":
                            args[i] = DateTime.Parse(argValue);
                            break;

                        case "char":
                            args[i] = Char.Parse(argValue);
                            break;

                        case "double":
                            args[i] = Double.Parse(argValue);
                            break;

                        case "decimal":
                            args[i] = Decimal.Parse(argValue);
                            break;

                        case "bool":
                            args[i] = Boolean.Parse(argValue);
                            break;

                        default:
                            args[i] = argValue;
                            break;
                    }
                }
            }

            return args;
        }
    }
}