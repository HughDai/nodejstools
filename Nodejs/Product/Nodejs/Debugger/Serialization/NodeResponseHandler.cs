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
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudioTools.Project;

namespace Microsoft.NodejsTools.Debugger.Serialization {
    class NodeResponseHandler : INodeResponseHandler {
        private readonly INodeEvaluationResultFactory _evaluationResultFactory;
        private readonly NodeModule _unknownModule = new NodeModule(null, -1, "<unknown>");

        /// <summary>
        /// Instantiates response message parser..
        /// </summary>
        /// <param name="evaluationResultFactory">Evaluation result factory.</param>
        public NodeResponseHandler(INodeEvaluationResultFactory evaluationResultFactory) {
            Utilities.ArgumentNotNull("evaluationResultFactory", evaluationResultFactory);

            _evaluationResultFactory = evaluationResultFactory;
        }

        /// <summary>
        /// Handles backtrace response message.
        /// </summary>
        /// <param name="thread">Thread.</param>
        /// <param name="message">Message.</param>
        /// <returns>Array of stack frames.</returns>
        public void ProcessBacktrace(NodeThread thread, Dictionary<int, NodeModule> modules, JsonValue message, Action<NodeStackFrame[]> successHandler) {
            Utilities.ArgumentNotNull("thread", thread);
            Utilities.ArgumentNotNull("message", message);

            // Extract scripts (if not provided)
            if (modules == null) {
                JsonArray refs = message.GetArray("refs");
                modules = GetScripts(thread.Process, refs);
            }

            // Extract frames
            JsonValue body = message["body"];
            JsonArray frames = body.GetArray("frames");
            if (frames == null) {
                if (successHandler != null) {
                    successHandler(new NodeStackFrame[] { });
                }
                return;
            }

            var stackFrames = new List<NodeStackFrame>(frames.Count);

            for (int i = 0; i < frames.Count; i++) {
                JsonValue frame = frames[i];

                // Create stack frame
                string name = GetFrameName(frame);
                var moduleId = frame["func"].GetValue<int>("scriptId");
                NodeModule module;
                if (!modules.TryGetValue(moduleId, out module)) {
                    module = _unknownModule;
                }

                int line = frame.GetValue<int>("line") + 1;
                var stackFrameId = frame.GetValue<int>("index");

                var stackFrame = new NodeStackFrame(thread, module, name, line, line, line, stackFrameId);

                // Locals
                JsonArray variables = frame.GetArray("locals");
                List<NodeEvaluationResult> locals = GetVariables(stackFrame, variables);

                // Arguments
                variables = frame.GetArray("arguments");
                List<NodeEvaluationResult> parameters = GetVariables(stackFrame, variables);

                stackFrame.Locals = locals;
                stackFrame.Parameters = parameters;

                stackFrames.Add(stackFrame);
            }

            if (successHandler != null) {
                successHandler(stackFrames.ToArray());
            }
        }

        /// <summary>
        /// Handles lookup response message.
        /// </summary>
        /// <param name="parent">Parent variable.</param>
        /// <param name="message">Message.</param>
        /// <returns>Array of evaluation results.</returns>
        public NodeEvaluationResult[] ProcessLookup(NodeEvaluationResult parent, JsonValue message) {
            Utilities.ArgumentNotNull("parent", parent);
            Utilities.ArgumentNotNull("message", message);

            // Retrieve references
            JsonArray refs = message.GetArray("refs");
            var references = new Dictionary<int, JsonValue>(refs.Count);
            for (int i = 0; i < refs.Count; i++) {
                JsonValue reference = refs[i];
                var id = reference.GetValue<int>("handle");
                references.Add(id, reference);
            }

            // Retrieve properties
            JsonValue body = message["body"];
            string handle = parent.Handle.ToString(CultureInfo.InvariantCulture);
            JsonValue objectData = body[handle];
            var properties = new List<NodeEvaluationResult>();

            JsonArray props = objectData.GetArray("properties");
            if (props != null) {
                for (int i = 0; i < props.Count; i++) {
                    JsonValue property = props[i];
                    var variableProvider = new NodeLookupVariable(parent, property, references);
                    NodeEvaluationResult result = _evaluationResultFactory.Create(variableProvider);
                    properties.Add(result);
                }
            }

            // Try to get prototype
            JsonValue prototype = objectData["protoObject"];
            if (prototype != null) {
                var variableProvider = new NodePrototypeVariable(parent, prototype, references);
                NodeEvaluationResult result = _evaluationResultFactory.Create(variableProvider);
                properties.Add(result);
            }

            return properties.ToArray();
        }

        /// <summary>
        /// Handles evaluate response message.
        /// </summary>
        /// <param name="stackFrame">Stack frame.</param>
        /// <param name="expression">Expression.</param>
        /// <param name="message">Message.</param>
        /// <returns>Evaluation result.</returns>
        public NodeEvaluationResult ProcessEvaluate(NodeStackFrame stackFrame, string expression, JsonValue message) {
            Utilities.ArgumentNotNull("stackFrame", stackFrame);
            Utilities.ArgumentNotNull("expression", expression);
            Utilities.ArgumentNotNull("message", message);

            JsonValue body = message["body"];
            var variableProvider = new NodeEvaluationVariable(stackFrame, expression, body);
            return _evaluationResultFactory.Create(variableProvider);
        }

        private static string GetFrameName(JsonValue frame) {
            JsonValue func = frame["func"];
            var framename = func.GetValue<string>("name");
            if (string.IsNullOrEmpty(framename)) {
                framename = func.GetValue<string>("inferredName");
            }
            if (string.IsNullOrEmpty(framename)) {
                framename = NodeVariableType.AnonymousFunction;
            }
            return framename;
        }

        private static Dictionary<int, NodeModule> GetScripts(NodeDebugger debugger, JsonArray references) {
            var scripts = new Dictionary<int, NodeModule>(references.Count);
            for (int i = 0; i < references.Count; i++) {
                JsonValue reference = references[i];
                var scriptId = reference.GetValue<int>("id");
                var filename = reference.GetValue<string>("name");

                scripts.Add(scriptId, new NodeModule(debugger, scriptId, filename));
            }
            return scripts;
        }

        private List<NodeEvaluationResult> GetVariables(NodeStackFrame stackFrame, JsonArray variables) {
            var results = new List<NodeEvaluationResult>(variables.Count);
            for (int i = 0; i < variables.Count; i++) {
                var variableProvider = new NodeBacktraceVariable(stackFrame, variables[i]);
                NodeEvaluationResult result = _evaluationResultFactory.Create(variableProvider);
                results.Add(result);
            }
            return results.ToList();
        }
    }
}