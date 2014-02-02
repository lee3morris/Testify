// ------------------------------------------------------------------------------
//  <auto-generated>
//    Generated by Xsd2Code. Version 3.4.0.38967
//    <NameSpace>Leem.Testify.Domain</NameSpace><Collection>List</Collection><codeType>CSharp</codeType><EnableDataBinding>False</EnableDataBinding><EnableLazyLoading>False</EnableLazyLoading><TrackingChangesEnable>False</TrackingChangesEnable><GenTrackingClasses>False</GenTrackingClasses><HidePrivateFieldInIDE>True</HidePrivateFieldInIDE><EnableSummaryComment>True</EnableSummaryComment><VirtualProp>False</VirtualProp><IncludeSerializeMethod>True</IncludeSerializeMethod><UseBaseClass>True</UseBaseClass><GenBaseClass>True</GenBaseClass><GenerateCloneMethod>False</GenerateCloneMethod><GenerateDataContracts>False</GenerateDataContracts><CodeBaseTag>Net40</CodeBaseTag><SerializeMethodName>Serialize</SerializeMethodName><DeserializeMethodName>Deserialize</DeserializeMethodName><SaveToFileMethodName>SaveToFile</SaveToFileMethodName><LoadFromFileMethodName>LoadFromFile</LoadFromFileMethodName><GenerateXMLAttributes>True</GenerateXMLAttributes><EnableEncoding>False</EnableEncoding><AutomaticProperties>True</AutomaticProperties><GenerateShouldSerialize>False</GenerateShouldSerialize><DisableDebug>True</DisableDebug><PropNameSpecified>Default</PropNameSpecified><Encoder>UTF8</Encoder><CustomUsings></CustomUsings><ExcludeIncludedTypes>False</ExcludeIncludedTypes><EnableInitializeFields>True</EnableInitializeFields>
//  </auto-generated>
// ------------------------------------------------------------------------------
namespace Leem.Testify.Domain
{
    using System;
    using System.Diagnostics;
    using System.Xml.Serialization;
    using System.Collections;
    using System.Xml.Schema;
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;


    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.18060")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlRootAttribute("test-results", Namespace = "", IsNullable = false)]
    public partial class resultType
    {

        [EditorBrowsable(EditorBrowsableState.Never)]
        private environmentType environmentField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private cultureinfoType cultureinfoField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private testsuiteType testsuiteField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string nameField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private decimal totalField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private decimal errorsField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private decimal failuresField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private decimal inconclusiveField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private decimal notrunField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private decimal ignoredField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private decimal skippedField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private decimal invalidField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string dateField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string timeField;

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal total { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal errors { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal failures { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal inconclusive { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute("not-run")]
        public decimal notrun { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal ignored { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal skipped { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal invalid { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string date { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string time { get; set; }


        /// <summary>
        /// resultType class constructor
        /// </summary>
        public resultType()
        {
            this.testsuiteField = new testsuiteType();
            this.cultureinfoField = new cultureinfoType();
            this.environmentField = new environmentType();
        }

        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public environmentType environment
        {
            get
            {
                return this.environmentField;
            }
            set
            {
                this.environmentField = value;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("culture-info", Order = 1)]
        public cultureinfoType cultureinfo
        {
            get
            {
                return this.cultureinfoField;
            }
            set
            {
                this.cultureinfoField = value;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("test-suite", Order = 2)]
        public testsuiteType testsuite
        {
            get
            {
                return this.testsuiteField;
            }
            set
            {
                this.testsuiteField = value;
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.18060")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = true)]
    public partial class environmentType
    {

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string nunitversionField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string clrversionField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string osversionField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string platformField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string cwdField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string machinenameField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string userField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string userdomainField;

        [System.Xml.Serialization.XmlAttributeAttribute("nunit-version")]
        public string nunitversion { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute("clr-version")]
        public string clrversion { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute("os-version")]
        public string osversion { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string platform { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string cwd { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute("machine-name")]
        public string machinename { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string user { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute("user-domain")]
        public string userdomain { get; set; }

    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.18060")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(TypeName = "test-caseType")]
    [System.Xml.Serialization.XmlRootAttribute("test-caseType", Namespace = "", IsNullable = true)]
    public partial class testcaseType
    {
        private List<categoryType> categoriesField;
        private List<propertyType> propertiesField;
        private object itemField;
        private string nameField;
        private string descriptionField;
        private string successField;
        private string timeField;
        private string executedField;
        private string assertsField;
        private string resultField;

        [System.Xml.Serialization.XmlElementAttribute("failure", typeof(failureType), Order = 2)]
        [System.Xml.Serialization.XmlElementAttribute("reason", typeof(reasonType), Order = 2)]
        public object Item { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string description { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string success { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string time { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string executed { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string asserts { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string result { get; set; }


        /// <summary>
        /// testcaseType class constructor
        /// </summary>
        public testcaseType()
        {
            this.propertiesField = new List<propertyType>();
            this.categoriesField = new List<categoryType>();
        }

        [System.Xml.Serialization.XmlArrayAttribute(Order = 0)]
        [System.Xml.Serialization.XmlArrayItemAttribute("category", IsNullable = false)]
        public List<categoryType> categories
        {
            get
            {
                return this.categoriesField;
            }
            set
            {
                this.categoriesField = value;
            }
        }

        [System.Xml.Serialization.XmlArrayAttribute(Order = 1)]
        [System.Xml.Serialization.XmlArrayItemAttribute("property", IsNullable = false)]
        public List<propertyType> properties
        {
            get
            {
                return this.propertiesField;
            }
            set
            {
                this.propertiesField = value;
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.18060")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = true)]
    public partial class categoryType
    {

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string nameField;

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name { get; set; }

    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.18060")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = true)]
    public partial class propertyType //: EntityBase<propertyType>
    {

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string nameField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string valueField;

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string value { get; set; }

    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.18060")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = true)]
    public partial class failureType
    {

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string messageField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string stacktraceField;

        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string message { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("stack-trace", Order = 1)]
        public string stacktrace { get; set; }

    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.18060")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = true)]
    public partial class reasonType
    {

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string messageField;

        [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
        public string message { get; set; }

    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.18060")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = true)]
    public partial class resultsType 
    {

         private List<object> itemsField;

        /// <summary>
        /// resultsType class constructor
        /// </summary>
        public resultsType()
        {
            this.itemsField = new List<object>();
        }

        [System.Xml.Serialization.XmlElementAttribute("test-case", typeof(testcaseType), Order = 0)]
        [System.Xml.Serialization.XmlElementAttribute("test-suite", typeof(testsuiteType), Order = 0)]
        public List<object> Items 
        {
            get
            {
                return this.itemsField;
            }
            set
            {
                this.itemsField = value;
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.18060")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(TypeName = "test-suiteType")]
    [System.Xml.Serialization.XmlRootAttribute("test-suiteType", Namespace = "", IsNullable = true)]
    public partial class testsuiteType
    {

        private List<categoryType> categoriesField;
        private List<propertyType> propertiesField;
        private object itemField;
        private resultsType resultsField;
        private string typeField;
        private string nameField;
        private string descriptionField;

        private string successField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string timeField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string executedField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string assertsField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string resultField;

        [System.Xml.Serialization.XmlElementAttribute("failure", typeof(failureType), Order = 2)]
        [System.Xml.Serialization.XmlElementAttribute("reason", typeof(reasonType), Order = 2)]
        public object Item { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string description { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string success { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string time { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string executed { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string asserts { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string result { get; set; }


        /// <summary>
        /// testsuiteType class constructor
        /// </summary>
        public testsuiteType()
        {
            this.resultsField = new resultsType();
            this.propertiesField = new List<propertyType>();
            this.categoriesField = new List<categoryType>();
        }

        [System.Xml.Serialization.XmlArrayAttribute(Order = 0)]
        [System.Xml.Serialization.XmlArrayItemAttribute("category", IsNullable = false)]
        public List<categoryType> categories
        {
            get
            {
                return this.categoriesField;
            }
            set
            {
                this.categoriesField = value;
            }
        }

        [System.Xml.Serialization.XmlArrayAttribute(Order = 1)]
        [System.Xml.Serialization.XmlArrayItemAttribute("property", IsNullable = false)]
        public List<propertyType> properties
        {
            get
            {
                return this.propertiesField;
            }
            set
            {
                this.propertiesField = value;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute(Order = 3)]
        public resultsType results
        {
            get
            {
                return this.resultsField;
            }
            set
            {
                this.resultsField = value;
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.18060")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(TypeName = "culture-infoType")]
    [System.Xml.Serialization.XmlRootAttribute("culture-infoType", Namespace = "", IsNullable = true)]
    public partial class cultureinfoType
    {

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string currentcultureField;

        [EditorBrowsable(EditorBrowsableState.Never)]
        private string currentuicultureField;

        [System.Xml.Serialization.XmlAttributeAttribute("current-culture")]
        public string currentculture { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute("current-uiculture")]
        public string currentuiculture { get; set; }

    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.18060")]
    [System.SerializableAttribute()]

    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = true)]
    public partial class categoriesType
    {

        [EditorBrowsable(EditorBrowsableState.Never)]
        private List<categoryType> categoryField;

        /// <summary>
        /// categoriesType class constructor
        /// </summary>
        public categoriesType()
        {
            this.categoryField = new List<categoryType>();
        }

        [System.Xml.Serialization.XmlElementAttribute("category", Order = 0)]
        public List<categoryType> category
        {
            get
            {
                return this.categoryField;
            }
            set
            {
                this.categoryField = value;
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.18060")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = true)]
    public partial class propertiesType
    {

        [EditorBrowsable(EditorBrowsableState.Never)]
        private List<propertyType> propertyField;

        /// <summary>
        /// propertiesType class constructor
        /// </summary>
        public propertiesType()
        {
            this.propertyField = new List<propertyType>();
        }

        [System.Xml.Serialization.XmlElementAttribute("property", Order = 0)]
        public List<propertyType> property
        {
            get
            {
                return this.propertyField;
            }
            set
            {
                this.propertyField = value;
            }
        }
    }
}
