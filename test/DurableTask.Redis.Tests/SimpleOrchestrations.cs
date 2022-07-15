﻿//  ----------------------------------------------------------------------------------
//  Copyright Microsoft Corporation
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  ----------------------------------------------------------------------------------

namespace DurableTask.Test.Orchestrations
{
#pragma warning disable CA1725 // Parameter names should match base declaration
    using System;
    using System.Diagnostics.Contracts;
    using System.Threading.Tasks;
    using DurableTask.Core;

    public sealed class SimplestGetUserTask : TaskActivity<string, string>
    {
        protected override string Execute(TaskContext context, string input) => "Gabbar";
    }

    public class SimplestGreetingsOrchestration : TaskOrchestration<string, string>
    {
#pragma warning disable CA2211 // Non-constant fields should not be visible
        // HACK: This is just a hack to communicate result of orchestration back to test
        public static string Result;
#pragma warning restore CA2211 // Non-constant fields should not be visible

        public override async Task<string> RunTask(OrchestrationContext context, string input)
        {
            Contract.Assume(context is not null);
            string user = await context.ScheduleTask<string>(typeof(SimplestGetUserTask));
            int delayInSeconds = string.IsNullOrWhiteSpace(input) ? 0 : Int32.Parse(input);

            if (delayInSeconds > 0)
            {
                await context.CreateTimer<object>(context.CurrentUtcDateTime.Add(TimeSpan.FromSeconds(delayInSeconds)), null);
            }

            string greeting = await context.ScheduleTask<string>(typeof(SimplestSendGreetingTask), user);
            // This is a HACK to get unit test up and running.  Should never be done in actual code.
            Result = greeting;

            return greeting;
        }
    }

    public sealed class SimplestSendGreetingTask : TaskActivity<string, string>
    {
        protected override string Execute(TaskContext context, string user) => "Greeting send to " + user;
    }

    public class FanOutOrchestration : TaskOrchestration<int, int[]>
    {
#pragma warning disable CA2211 // Non-constant fields should not be visible
        // HACK: This is just a hack to communicate result of orchestration back to test
        public static int Result;
#pragma warning restore CA2211 // Non-constant fields should not be visible


        public override async Task<int> RunTask(OrchestrationContext context, int[] input)
        {
            Contract.Assume(context is not null);
            Contract.Assume(input is not null);
            Task<int>[] squareTasks = new Task<int>[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                squareTasks[i] = context.ScheduleTask<int>(typeof(SquareIntTask), input[i]);
            }
            int[] squares = await Task.WhenAll(squareTasks);
            Result = await context.ScheduleTask<int>(typeof(SumIntTask), squares);
            return Result;
        }
    }

    public sealed class SquareIntTask : TaskActivity<int, int>
    {
        protected override int Execute(TaskContext context, int numToSquare) => numToSquare * numToSquare;
    }

    public sealed class SumIntTask : TaskActivity<int[], long>
    {
        protected override long Execute(TaskContext context, int[] numsToAdd)
        {
            Contract.Assume(numsToAdd is not null);
            long sum = 0;
            foreach (int num in numsToAdd)
            {
                sum += num;
            }
            return sum;
        }
    }
}
