/**
 * @license
 * Visual Blocks Editor
 *
 * Copyright 2011 Google Inc.
 * https://developers.google.com/blockly/
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 * @fileoverview Core JavaScript library for Blockly.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Bridge;

namespace Blockly
{
	/// <summary>
	/// Allow for switching between one and zero based indexing for lists and text,
	/// one based by default.
	/// </summary>
	public class Blocks : Dictionary<string, Union<Type, string>>
	{
		public bool ONE_BASED_INDEXING = true;

		public Blocks()
		{
			Add(ColourPickerBlock.type_name, typeof(ColourPickerBlock));
			Add(ColourRandomBlock.type_name, typeof(ColourRandomBlock));
			Add(ColourRGBBlock.type_name, typeof(ColourRGBBlock));
			Add(ColourBlendBlock.type_name, typeof(ColourBlendBlock));
			Add(ListsCreateEmptyBlock.type_name, typeof(ListsCreateEmptyBlock));
			Add(ListsCreateWithBlock.type_name, typeof(ListsCreateWithBlock));
			Add(ListsCreateWithContainerBlock.type_name, typeof(ListsCreateWithContainerBlock));
			Add(ListsCreateWithItemBlock.type_name, typeof(ListsCreateWithItemBlock));
			Add(ListsRepeatBlock.type_name, typeof(ListsRepeatBlock));
			Add(ListsLengthBlock.type_name, typeof(ListsLengthBlock));
			Add(ListsIsEmptyBlock.type_name, typeof(ListsIsEmptyBlock));
			Add(ListsIndexOfBlock.type_name, typeof(ListsIndexOfBlock));
			Add(ListsGetIndexBlock.type_name, typeof(ListsGetIndexBlock));
			Add(ListsSetIndexBlock.type_name, typeof(ListsSetIndexBlock));
			Add(ListsGetSublistBlock.type_name, typeof(ListsGetSublistBlock));
			Add(ListsSortBlock.type_name, typeof(ListsSortBlock));
			Add(ListsSplitBlock.type_name, typeof(ListsSplitBlock));
			Add(ControlsIfBlock.type_name, typeof(ControlsIfBlock));
			Add(ControlsIfIfBlock.type_name, typeof(ControlsIfIfBlock));
			Add(ControlsIfElseIfBlock.type_name, typeof(ControlsIfElseIfBlock));
			Add(ControlsIfElseBlock.type_name, typeof(ControlsIfElseBlock));
			Add(LogicCompareBlock.type_name, typeof(LogicCompareBlock));
			Add(LogicOperationBlock.type_name, typeof(LogicOperationBlock));
			Add(LogicNegateBlock.type_name, typeof(LogicNegateBlock));
			Add(LogicBooleanBlock.type_name, typeof(LogicBooleanBlock));
			Add(LogicNullBlock.type_name, typeof(LogicNullBlock));
			Add(LogicTernaryBlock.type_name, typeof(LogicTernaryBlock));
			Add(ControlsRepeatExtBlock.type_name, typeof(ControlsRepeatExtBlock));
			Add(ControlsRepeatBlock.type_name, typeof(ControlsRepeatBlock));
			Add(ControlsWhileUntilBlock.type_name, typeof(ControlsWhileUntilBlock));
			Add(ControlsForBlock.type_name, typeof(ControlsForBlock));
			Add(ControlsForEachBlock.type_name, typeof(ControlsForEachBlock));
			Add(ControlsFlowStatementsBlock.type_name, typeof(ControlsFlowStatementsBlock));
			Add(MathNumberBlock.type_name, typeof(MathNumberBlock));
			Add(MathArithmeticBlock.type_name, typeof(MathArithmeticBlock));
			Add(MathSingleBlock.type_name, typeof(MathSingleBlock));
			Add(MathTrigBlock.type_name, typeof(MathTrigBlock));
			Add(MathConstantBlock.type_name, typeof(MathConstantBlock));
			Add(MathNumberPropertyBlock.type_name, typeof(MathNumberPropertyBlock));
			Add(MathChangeBlock.type_name, typeof(MathChangeBlock));
			Add(MathRoundBlock.type_name, typeof(MathRoundBlock));
			Add(MathOnListBlock.type_name, typeof(MathOnListBlock));
			Add(MathModuloBlock.type_name, typeof(MathModuloBlock));
			Add(MathConstrainBlock.type_name, typeof(MathConstrainBlock));
			Add(MathRandomIntBlock.type_name, typeof(MathRandomIntBlock));
			Add(MathRandomFloatBlock.type_name, typeof(MathRandomFloatBlock));
			Add(ProceduresDefnoreturnBlock.type_name, typeof(ProceduresDefnoreturnBlock));
			Add(ProceduresDefreturnBlock.type_name, typeof(ProceduresDefreturnBlock));
			Add(ProceduresMutatorcontainerBlock.type_name, typeof(ProceduresMutatorcontainerBlock));
			Add(ProceduresMutatorargBlock.type_name, typeof(ProceduresMutatorargBlock));
			Add(ProceduresCallnoreturnBlock.type_name, typeof(ProceduresCallnoreturnBlock));
			Add(ProceduresCallreturnBlock.type_name, typeof(ProceduresCallreturnBlock));
			Add(ProceduresIfreturnBlock.type_name, typeof(ProceduresIfreturnBlock));
			Add(TextBlock.type_name, typeof(TextBlock));
			Add(TextJoinBlock.type_name, typeof(TextJoinBlock));
			Add(TextCreateJoinContainerBlock.type_name, typeof(TextCreateJoinContainerBlock));
			Add(TextCreateJoinItemBlock.type_name, typeof(TextCreateJoinItemBlock));
			Add(TextAppendBlock.type_name, typeof(TextAppendBlock));
			Add(TextLengthBlock.type_name, typeof(TextLengthBlock));
			Add(TextIsEmptyBlock.type_name, typeof(TextIsEmptyBlock));
			Add(TextIndexOfBlock.type_name, typeof(TextIndexOfBlock));
			Add(TextCharAtBlock.type_name, typeof(TextCharAtBlock));
			Add(TextGetSubstringBlock.type_name, typeof(TextGetSubstringBlock));
			Add(TextChangeCaseBlock.type_name, typeof(TextChangeCaseBlock));
			Add(TextTrimBlock.type_name, typeof(TextTrimBlock));
			Add(TextPrintBlock.type_name, typeof(TextPrintBlock));
			Add(TextPromptExtBlock.type_name, typeof(TextPromptExtBlock));
			Add(TextPromptBlock.type_name, typeof(TextPromptBlock));
			Add(VariablesGetBlock.type_name, typeof(VariablesGetBlock));
			Add(VariablesSetBlock.type_name, typeof(VariablesSetBlock));
		}
	}

	public class RuntimeBlock : Blockly.BlockSvg
	{
		string json;

		public RuntimeBlock(Workspace workspace, string prototypeName, string json)
			: base(workspace, prototypeName)
		{
			this.json = json;
		}

		public override void init()
		{
			jsonInit((Dictionary<string, object>)JSON.Parse(json));
		}
	}
}
