﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Reflection;

namespace ReposData
{

    /// <summary>
    /// NonPublicColumnAttributeConvention
    /// This class is used to bind domain object 
    /// internal properties
    /// </summary>
    public sealed class NonPublicColumnAttributeConvention : Convention
    {

        public NonPublicColumnAttributeConvention()
        {
            Types().Having(NonPublicProperties)
                   .Configure((config, properties) =>
                   {
                       foreach (PropertyInfo prop in properties)
                       {
                           config.Property(prop);
                       }
                   });
        }

        private IEnumerable<PropertyInfo> NonPublicProperties(Type type)
        {

            var matchingProperties = type.GetProperties(BindingFlags.SetProperty 
                                            | BindingFlags.GetProperty 
                                            | BindingFlags.NonPublic 
                                            | BindingFlags.Instance)
                                         .Where(propInfo => propInfo.GetCustomAttributes(typeof(ColumnAttribute), true).Length > 0)
                                         .ToArray();
                        
            return matchingProperties.Length == 0 ? null : matchingProperties;
        }
    }
}
