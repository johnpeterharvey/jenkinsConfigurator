using System;

namespace jenkinsConfigurator
{
	public class Scenario
	{
		private String name;
		private String xmlConfig;

		public Scenario (String name, String xmlConfig)
		{
			this.name = name;
			this.xmlConfig = xmlConfig;
			Console.WriteLine("Added " + this.ToString());
		}

		public override String ToString()
		{
			return "Scenario [" + this.name  + "] - temperature [" + this.getTemperature() + "] - xmlConfig [" + this.xmlConfig + "] - priority [" + this.getPriority() + "]";
		}

		public override bool Equals(Object obj)
		{
			if (this.getName().Equals(obj.ToString()))
				return true;
			else
				return false;
		}

		public override int GetHashCode()
		{
			return this.name.GetHashCode();
		}

		public String getName()
		{
			return this.name;
		}

		public Temperature getTemperature()
		{
			if (this.name.StartsWith("Amb")) {
				return Temperature.Ambient;
			} else {
				return Temperature.Chilled;
			}
		}

		public String getXmlConfig()
		{
			return this.xmlConfig;
		}

		public int getPriority()
		{
			if (getTemperature().Equals(Temperature.Ambient)) {
				return 1;
			} else {
				return 50;
			}
		}
	}
}

