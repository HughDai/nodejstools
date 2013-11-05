﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System.Linq;
using TestUtilities.SharedProject;

namespace Microsoft.Nodejs.Tests.UI {
    public class NodejsProjectTest : SharedProjectTest {
        public static ProjectType NodejsProject = ProjectTypes.First(x => x.ProjectExtension == ".njsproj");

        public static ProjectDefinition Project(string name, params ProjectContentGenerator[] items) {
            return new ProjectDefinition(name, NodejsProject, items);
        }
    }
}