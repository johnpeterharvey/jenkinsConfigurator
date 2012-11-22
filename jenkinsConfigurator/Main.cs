using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace jenkinsConfigurator
{
	class MainClass
	{
		private static String jenkinsCLI = "/home/ateam/.jenkins/jenkins-cli.jar";
		private static String palletDirectory = "/home/ateam/.jenkins/inboundPalletSource/src/scenario/resources/com/ocado/atlas/inbound/pallet/scenarios";
		private static String jobSkeleton = "/home/ateam/.jenkins/configSkeleton.xml";
		private static String jobDirectory = "/home/ateam/.jenkins/jobs/";
		private static String inboundPalletSkeleton = "/home/ateam/.jenkins/configIPSkeleton.xml";
		private static String logFile = "/home/ateam/.jenkins/log.txt";

		private static String jenkinsURL = "http://localhost:8080";

		private static StreamWriter logWriter = null;

		public static void Main (string[] args)
		{
			ArrayList scenarios = getListOfScenarios(palletDirectory);

			ArrayList existingJenkinsJobs = getJenkinsJobs();

			deleteRemovedScenarios(scenarios, existingJenkinsJobs, jenkinsCLI);

			generateScenarioJobs(scenarios, jobSkeleton, jobDirectory);

			generateInboundPalletConfig(scenarios, inboundPalletSkeleton, jobDirectory);

			//triggerScenarioRuns(scenarios);

			bounceJenkins();


			logWriter.Close();
		}

		private static void deleteRemovedScenarios (ArrayList scenarios, ArrayList jenkinsJobs, String jenkinsCLI)
		{
			Regex ex = new Regex("Update.*|Manage.*|Jenkins.*|InboundPallet|Clear.*");
			foreach (String node in jenkinsJobs) {
				Match mt = ex.Match(node);
				if (!mt.Success && !scenarios.Contains(node)) {
					log("Deleting job " + node);
					Process.Start("/usr/bin/java", " -jar " + jenkinsCLI + " -s " + jenkinsURL + " delete-job " + node);
				}
			}
		}

		private static ArrayList getListOfScenarios(String baseCodeDirectory)
		{
			ArrayList scenariosFound = new ArrayList();

			foreach (String temp in new String[] {"ambient_scenarios", "chilled_scenarios"}) {
				String[] scenarios = Directory.GetFiles(baseCodeDirectory + "/" + temp);
				log("Got " + scenarios.Length + " scenarios in " + temp);

				foreach (String scenarioFile in scenarios) {
					StreamReader reader = new StreamReader(scenarioFile);

					Match xmlFile = Regex.Match(reader.ReadToEnd(), "([^ ]*xml)");
					reader.Close();

					scenariosFound.Add(new Scenario(Path.GetFileNameWithoutExtension(scenarioFile), xmlFile.Groups[1].Value));
				}
			}

			return scenariosFound;
		}

		private static void generateInboundPalletConfig(ArrayList scenarios, String skeletonPath, String jobDirectory)
		{
			log("Writing config for InboundPallet");
			ArrayList scenarioNames = new ArrayList();
			foreach (Scenario s in scenarios)
				scenarioNames.Add(s.getName());

			String childList = String.Join(", ", scenarioNames.ToArray());
			log("\tChildList " + childList);
			StreamReader readSkeleton = new StreamReader(skeletonPath);
			String config = readSkeleton.ReadToEnd().Replace("$children", childList);

			
			if (!Directory.Exists(jobDirectory + "InboundPallet"))
				Directory.CreateDirectory(jobDirectory + "InboundPallet");

			StreamWriter configWriter = new StreamWriter(jobDirectory + "InboundPallet/config.xml", false);
			configWriter.Write(config);
			configWriter.Close();
		}

		private static void generateScenarioJobs(ArrayList scenarios, String skeletonFile, String jobDirectory)
		{
			StreamReader skeletonReader = new StreamReader(skeletonFile);
			String skeleton = skeletonReader.ReadToEnd();
			skeletonReader.Close();

			foreach (Scenario scenario in scenarios) {
				log("Scenario " + scenario.getName());
				log("\tUsing priority " + scenario.getPriority() + ", and temperture " + scenario.getTemperature().ToString());
				String newConfig = skeleton.Replace("$priority", scenario.getPriority().ToString()).Replace("$temperature", scenario.getTemperature().ToString());
				if (scenario.getXmlConfig() == null || scenario.getXmlConfig().Equals("")) {
					log("\tReplacing $scenario with " + scenario.getName());
					newConfig = newConfig.Replace("$scenario", scenario.getName());
				}
				else {
					log("\tReplacing $scenario with " + scenario.getName() + " -DcontainerConfig=" + scenario.getXmlConfig());
					newConfig = newConfig.Replace("$scenario", scenario.getName() + " -DcontainerConfig=" + scenario.getXmlConfig());
				}

				if (!Directory.Exists(jobDirectory + scenario.getName()))
					Directory.CreateDirectory(jobDirectory + scenario.getName());

				log("Writing config for scenario " + scenario.getName());
				StreamWriter configWriter = new StreamWriter(jobDirectory + scenario.getName() + "/config.xml", false);
				configWriter.Write(newConfig);
				configWriter.Close();
			}
		}

		private static void bounceJenkins()
		{
			log("Bouncing jenkins");

			Process bouncy = new Process();
			bouncy.StartInfo.FileName = "/usr/bin/java";
			bouncy.StartInfo.Arguments = " -jar " + jenkinsCLI + " -i /home/ateam/.ssh/id_rsa -s " + jenkinsURL + " reload-configuration";
			bouncy.Start();
			bouncy.WaitForExit();
			bouncy.Close();

			log("Bounce requested");
		}

		private static ArrayList getJenkinsJobs()
		{
			XmlDocument reader = new XmlDocument();
			reader.Load(jenkinsURL + "/api/xml");

			XmlNodeList jobNodes = reader.SelectNodes("//hudson/job/name/text()");

			ArrayList returnArray = new ArrayList();
			foreach (XmlNode node in jobNodes) {
				returnArray.Add(node.Value);
			}

			return returnArray;
		}

		private static void log(String log)
		{
			Console.WriteLine(log);
			if (logWriter == null)
				logWriter = new StreamWriter(logFile, false);

			logWriter.WriteLine(System.DateTime.Now + " " + log);
			logWriter.Flush();
		}

		public static void triggerInboundPalletBuild()
		{
			log("Requesting InboundPallet build");

			Process.Start("/usr/bin/java", " -jar " + jenkinsCLI + " -i /home/ateam/.ssh/id_rsa -s " + jenkinsURL + " build InboundPallet");

			log("Called build InboundPallet");
		}

		private static void triggerScenarioRuns(ArrayList scenarios)
		{

			log("About to trigger each scenario");

			foreach (Scenario s in scenarios) {
				log("\t->" + s.getName());
				Process.Start("/usr/bin/java", " -jar " + jenkinsCLI + " -i /home/ateam/.ssh/id_rsa -s " + jenkinsURL + " build " + s.getName());
			}
			log("Done triggering scenarios");
						Thread.Sleep(10000);
		}
	}
}
