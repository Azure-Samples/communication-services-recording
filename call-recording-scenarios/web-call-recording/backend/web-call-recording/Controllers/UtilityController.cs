using System.Xml.Linq;
using System.Xml;
using Azure.Communication.CallAutomation;

namespace web_call_recording.Controllers
{
    [Route("api/utility")]
    [ApiController]
    public class UtilityController : ControllerBase
    {
        private readonly ILogger logger;

        public UtilityController(ILogger<UtilityController> logger)
        {
            this.logger = logger;
        }

        [HttpGet]
        [Route("get/sdk/version")]
        public async Task<ActionResult> GetPackageLatestVersion()
        {
            string projectFilePath = $"{Directory.GetCurrentDirectory()}\\communication-services-recording.csproj";
            string packageName = "Azure.Communication.CallAutomation";
            var result = new Dictionary<string, string>();
            try
            {
                XDocument doc = XDocument.Load(projectFilePath);

                var packageReference = doc.Descendants("PackageReference")
                    .FirstOrDefault(pr => pr.Attribute("Include")?.Value == packageName);

                if (packageReference != null)
                {
                    string packageVersion = packageReference.Attribute("Version")?.Value;
                   
                    result.Add("packageName", packageName);
                    result.Add("packageVersion", packageVersion);
                }
            }
            catch (XmlException e)
            {
                this.logger.LogInformation($"Error parsing {projectFilePath}: {e.Message}");
            }

            return Ok(result);
        }
    }
}
