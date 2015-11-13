using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using TeamCityApi.Domain;
using TeamCityApi.Helpers;
using TeamCityApi.Locators;

namespace TeamCityApi.Clients
{
    public interface IBuildConfigXmlClient
    {   
        void Track(IBuildConfigXml buildConfigXml);
        IBuildConfigXml Read(string buildConfigId);
        IBuildConfigXml ReadAsOf(string buildConfigId, DateTime asOfDateTime);
        void EndSetOfChanges();

    }

    public class BuildConfigXmlClient : IBuildConfigXmlClient
    {
        private readonly IVcsRootHelper _vcsRootHelper;
        private readonly List<IBuildConfigXml> _buildConfigXmls = new List<IBuildConfigXml>();
        
        public BuildConfigXmlClient(IVcsRootHelper vcsRootHelper)
        {
            _vcsRootHelper = vcsRootHelper;
        }

        public void Track(IBuildConfigXml buildConfigXml)
        {
            _buildConfigXmls.Add(buildConfigXml);
        }

        public IBuildConfigXml Read(string buildConfigId)
        {
            //todo: clone settings repo
            //todo: find file by buildConfigId
            //todo: read contents to XML doc

            var buildConfigXml = new BuildConfigXml(this);


            _buildConfigXmls.Add(buildConfigXml);

            throw new NotImplementedException();

            //return buildConfigXml;
        }

        public IBuildConfigXml ReadAsOf(string buildConfigId, DateTime asOfDateTime)
        {
            //todo: clone settings, find file by buildConfigId, readit.


            var buildConfigXml = new BuildConfigXml(this);
            _buildConfigXmls.Add(buildConfigXml);

            throw new NotImplementedException();

            //return buildConfigXml;
        }

        public void EndSetOfChanges()
        {
            foreach (var buildConfigXml in _buildConfigXmls)
            {
                //todo: read from buildConfigXml.Xml, same to file
            }

            //todo: commit

            //todo: push

            throw new NotImplementedException();
        }
    }
}