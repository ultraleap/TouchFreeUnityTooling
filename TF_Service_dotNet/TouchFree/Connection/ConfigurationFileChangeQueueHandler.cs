﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Ultraleap.TouchFree.Library.Configuration;

namespace Ultraleap.TouchFree.Library.Connection
{
    public class ConfigurationFileChangeQueueHandler : BaseConfigurationChangeQueueHandler
    {
        public override ActionCode ActionCode => ActionCode.SET_CONFIGURATION_FILE;

        public ConfigurationFileChangeQueueHandler(UpdateBehaviour _updateBehaviour, IClientConnectionManager _clientMgr) : base(_updateBehaviour, _clientMgr)
        {
        }

        protected override void Handle(string _content)
        {
            // Validate the incoming change
            ResponseToClient response = ValidateConfigChange(_content);

            if (response.status == "Success")
            {
                // Try saving config
                // If not work, return error
                // If work, send response from above
                try
                {
                    ChangeConfigFile(_content);
                }
                catch (UnauthorizedAccessException _)
                {
                    // Return some response indicating access authorisation issues
                    string errorMsg = "Did not have appropriate file access to modify the config file(s).";
                    response = new ResponseToClient(response.requestID, "Failed", errorMsg, _content);
                }
            }

            clientMgr.SendConfigFileChangeResponse(response);
        }

        void ChangeConfigFile(string _content)
        {
            // Get the current state of the config file(s)
            InteractionConfig intFromFile = InteractionConfigFile.LoadConfig();
            PhysicalConfig physFromFile = PhysicalConfigFile.LoadConfig();

            var contentJson = JObject.Parse(_content);

            string physicalChanges = contentJson["physical"].ToString();
            string interactionChanges = contentJson["interaction"].ToString();

            if (physicalChanges != string.Empty)
            {
                JsonConvert.PopulateObject(physicalChanges, physFromFile);
                PhysicalConfigFile.SaveConfig(physFromFile);
            }

            if (interactionChanges != string.Empty)
            {
                JsonConvert.PopulateObject(interactionChanges, intFromFile);
                InteractionConfigFile.SaveConfig(intFromFile);
            }
        }
    }
}
