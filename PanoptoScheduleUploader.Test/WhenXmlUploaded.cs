using System;
using System.Linq;
using PanoptoScheduleUploader.Core;
using System.IO;
using NUnit.Framework;

namespace PanoptoScheduleUploader.Test
{
    [TestFixture]
    public class WhenXmlUploaded
    {
        private readonly string _assemblyDirectory;

        public WhenXmlUploaded()
        {
            var assemblyPath = System.Reflection.Assembly.GetAssembly(typeof(WhenXmlUploaded)).Location;
            this._assemblyDirectory = Path.GetDirectoryName(assemblyPath);
        }

        [Test]
        public void AndHasAllRequiredAttributes_ThenRecordingObjectCreated()
        {
            var expectedTemplateId = new Guid("175C9F73-E974-4FCA-B8A1-170681040219");
            var xmlParser = new RecorderScheduleXmlParser($"{this._assemblyDirectory}\\validXml.xml");
            var recordings = xmlParser.ExtractRecordings();

            Assert.IsTrue(recordings.All(x => !string.IsNullOrEmpty(x.Title)));
            Assert.IsTrue(recordings.Any(x => x.TemplateId == expectedTemplateId));
        }

        [Test]
        public void AndRequiredAttributesMissing_ThenElementListedAsInvalid()
        {
            Exception expectedException = null;
            try
            {
                var xmlParser = new RecorderScheduleXmlParser($"{this._assemblyDirectory}\\validXml_MissingAttribute.xml");
                xmlParser.ExtractRecordings();
            }
            catch (Exception exception)
            {
                expectedException = exception;
            }

            Assert.NotNull(expectedException);
            Assert.That(expectedException is Exception);
        }

        [Test]
        public void AndRequiredAttributesEmpty_ThenElementListedAsInvalid()
        {
            Exception expectedException = null;
            try
            {
                var xmlParser = new RecorderScheduleXmlParser($"{this._assemblyDirectory}\\validXml_EmptyAttribute.xml");
                xmlParser.ExtractRecordings();
            }
            catch (Exception exception)
            {
                expectedException = exception;
            }

            Assert.NotNull(expectedException);
            Assert.That(expectedException is Exception);
        }

        [Test]
        public void AndTemplateIdIsMalformed_ThenElementListedAsInvalid()
        {
            Exception expectedException = null;
            try
            {
                var xmlParser = new RecorderScheduleXmlParser($"{this._assemblyDirectory}\\validXml_BadTemplateId.xml");
                xmlParser.ExtractRecordings();
            }
            catch (Exception exception)
            {
                expectedException = exception;
            }

            Assert.NotNull(expectedException);
            Assert.That(expectedException is Exception);
        }
    }
}
