﻿//*********************************************************//
//    Copyright (c) Microsoft. All rights reserved.
//    
//    Apache 2.0 License
//    
//    You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//    
//    Unless required by applicable law or agreed to in writing, software 
//    distributed under the License is distributed on an "AS IS" BASIS, 
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or 
//    implied. See the License for the specific language governing 
//    permissions and limitations under the License.
//
//*********************************************************//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.VisualStudio.TemplateWizard;

namespace Microsoft.NodejsTools.ProjectWizard {
    /// <summary>
    /// Provides a project wizard extension which will optionally do an
    /// npm install after the project is created.
    /// </summary>
    public sealed class NpmWizardExtension : IWizard {
        #region IWizard Members

        public void BeforeOpeningFile(EnvDTE.ProjectItem projectItem) {
        }

        public void ProjectFinishedGenerating(EnvDTE.Project project) {
            Debug.Assert(project.Object != null);
            Debug.Assert(project.Object is INodePackageModulesCommands);
            // prompt the user to install dependencies
            var shouldDoInstall = MessageBox.Show(@"The newly created project has dependencies defined in package.json.

Do you want to run npm install to get the dependencies now?

This operation will run in the background.  
Results of this operation are available in the Output window.",
                "Node.js Tools for Visual Studio",
                MessageBoxButtons.YesNo
            );

            if (shouldDoInstall == DialogResult.Yes) {
                var t = ((INodePackageModulesCommands)project.Object).InstallMissingModulesAsync();
            }

        }

        public void ProjectItemFinishedGenerating(EnvDTE.ProjectItem projectItem) {
        }

        public void RunFinished() {
        }

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams) {
        }

        public bool ShouldAddProjectItem(string filePath) {
            return true;
        }

        #endregion
    }
}
