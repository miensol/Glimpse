﻿using System.Collections.Generic;
using System.Linq;
using Glimpse.Ado.Message;
using Glimpse.Ado.Model;
using Glimpse.Ado.Tab.Support;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Extensions;
using Glimpse.Core.Tab.Assist;

namespace Glimpse.Ado.Tab
{
    public class SQL : TabBase, ITabSetup, IKey, ITabLayout, IDocumentation, ILayoutControl
    {
        public override string Name
        {
            get { return "SQL"; }
        }

        public string Key
        {
            get { return "glimpse_sql"; }
        }

        public string DocumentationUri
        {
            get { return "http://getglimpse.com/Help/SQL-Tab"; }
        }

        public void Setup(ITabSetupContext context)
        {
            context.PersistMessages<AdoMessage>();
        }

        public object GetLayout()
        {
            return Layout;
        }

        public bool KeysHeadings
        {
            get { return true; }
        }

        public override object GetData(ITabContext context)
        {
            var sanitizer = new CommandSanitizer();
            var messages = context.GetMessages<AdoMessage>().ToList();
            var aggregator = new MessageAggregator(messages);
            var queryMetadata = aggregator.Aggregate();

            if (queryMetadata == null)
            {
                return null;
            }

            var connections = new List<object[]> { new object[] { "Commands per Connection", "Duration" } };

            foreach (var connection in queryMetadata.Connections.Values)
            {
                if (connection.Commands.Count == 0 && connection.Transactions.Count == 0)
                {
                    continue;
                }

                var commands = new List<object[]> { new object[] { "Transaction Start", "Ordinal", "Command", "Parameters", "Records", "Duration", "Offset", "Transaction End", "Errors" } };
                var commandCount = 1;
                foreach (var command in connection.Commands.Values)
                {
                    //Transaction Start
                    List<object[]> headTransaction = null;
                    if (command.HeadTransaction != null)
                    {
                        headTransaction = new List<object[]> { new object[] { "\t▼ Transaction - Started", "Isolation Level - " + command.HeadTransaction.IsolationLevel } };
                        if (!command.HeadTransaction.Committed.HasValue)
                        {
                            headTransaction.Add(new object[] { "", "Transaction was never completed", "error" });
                        }
                    }

                    //Transaction Finish
                    List<object[]> tailTransaction = null;
                    if (command.TailTransaction != null)
                    {
                        tailTransaction = new List<object[]> { new object[] { "\t▲ Transaction - Finished", "Status - " + (command.TailTransaction.Committed.GetValueOrDefault() ? "Committed" : "Rollbacked") } };
                    }

                    //Parameters
                    List<object[]> parameters = null;
                    if (command.Parameters.Count > 0)
                    {
                        parameters = new List<object[]> { new object[] { "Name", "Value", "Type", "Size" } };
                        foreach (var parameter in command.Parameters)
                        {
                            parameters.Add(new[] { parameter.Name, parameter.Value, parameter.Type, parameter.Size });
                        }
                    }

                    //Exception
                    List<object[]> errors = null;
                    if (command.Exception != null)
                    {
                        var exception = command.Exception.GetBaseException();
                        var exceptionName = command.Exception != exception ? command.Exception.Message + ": " + exception.Message : exception.Message;

                        errors = new List<object[]> { new object[] { "Error", "Stack" }, new object[] { exceptionName, exception.StackTrace } };
                    }

                    //Commands
                    var records = command.RecordsAffected == null || command.RecordsAffected < 0 ? command.TotalRecords : command.RecordsAffected;

                    var status = errors != null ? "error" : (command.IsDuplicate ? "warn" : string.Empty);
                    commands.Add(new object[] { headTransaction, string.Format("{0}{1}", command.HasTransaction ? "\t\t\t" : "", commandCount++), sanitizer.Process(command.Command, command.Parameters), parameters, records, command.Duration, command.Offset, tailTransaction, errors, status });
                }

                connections.Add(new[] { commands, connection.Duration.HasValue ? (object)connection.Duration.Value : null });
            }

            if (connections.Count > 1)
            {
                SqlStatistics sqlStatistics = SqlStatisticsCalculator.Caluculate(queryMetadata);

                return new Dictionary<string, object>
                {
                    { "SQL Statistics", new object[]{ new{ sqlStatistics.ConnectionCount, sqlStatistics.QueryCount, sqlStatistics.TransactionCount, sqlStatistics.QueryExecutionTime, sqlStatistics.ConnectionOpenTime }}}, 
                    { "Queries", connections }
                };
            }

            return null;
        }

        private static readonly object Layout =
            TabLayout.Create()
                .Cell("SQL Statistics", TabLayout.Create().Row(r =>
                {
                    r.Cell("connectionCount").WidthInPixels(150).WithTitle("# Connections");
                    r.Cell("queryCount").WidthInPixels(150).WithTitle("# Queries");
                    r.Cell("transactionCount").WidthInPixels(150).WithTitle("# Transactions");
                    r.Cell("queryExecutionTime").WidthInPixels(250).Suffix(" ms").Class("mono").WithTitle("Total query execution time");
                    r.Cell("connectionOpenTime").Suffix(" ms").Class("mono").WithTitle("Total connection open time");
                }))

                .Cell("Queries", TabLayout.Create().Row(r =>
                {
                    r.Cell(0).DisablePreview().SetLayout(TabLayout.Create().Row(x =>
                            x.Cell(0).SpanColumns(6).DisablePreview().AsMinimalDisplay().SetLayout(TabLayout.Create().Row(y =>
                            {
                                y.Cell(0).WidthInPixels(150);
                                y.Cell(1);
                            }))).Row(x =>
                            {
                                x.Cell(1).WidthInPixels(55);
                                x.Cell(2).AsCode(CodeType.Sql).DisablePreview();
                                x.Cell(3).WidthInPercent(25).DisablePreview();
                                x.Cell(4).WidthInPixels(60);
                                x.Cell(5).WidthInPixels(100).Suffix(" ms").Class("mono");
                                x.Cell(6).WidthInPixels(100).Prefix("T+ ").Suffix(" ms").Class("mono");
                            }).Row(x =>
                            x.Cell(8).SpanColumns(6).DisablePreview().AsMinimalDisplay().SetLayout(TabLayout.Create().Row(y =>
                            {
                                y.Cell(0).WidthInPercent(20);
                                y.Cell(1).Class("mono").DisablePreview();
                            }))).Row(x =>
                            x.Cell(7).SpanColumns(6).DisablePreview().AsMinimalDisplay().SetLayout(TabLayout.Create().Row(y =>
                            {
                                y.Cell(0).WidthInPixels(150);
                                y.Cell(1);
                            }))));
                    r.Cell(1).WidthInPixels(75).Suffix(" ms").Class("mono");
                }))
                .Build();
    }
}