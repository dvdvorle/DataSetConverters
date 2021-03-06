﻿namespace Json.Net.DataSetConverters
open Newtonsoft.Json
open System.Data
open JsonSerializationExtensions
open Newtonsoft.Json.Serialization
open System.Globalization
open System
open PropertyCollectionExtensions

type DataTableConverter() =
    inherit JsonConverter()

    [<Literal>]
    static let ObjectName = "DataTable"
    [<Literal>]
    static let CaseSensitive = "CaseSensitive"
    [<Literal>]
    static let DisplayExpression = "DisplayExpression"
    [<Literal>]
    static let Locale = "Locale"
    [<Literal>]
    static let MinimumCapacity = "MinimumCapacity"
    [<Literal>]
    static let Namespace = "Namespace"
    [<Literal>]
    static let Prefix = "Prefix"
    [<Literal>]
    static let RemotingFormat = "RemotingFormat"
    [<Literal>]
    static let TableName = "TableName"
    [<Literal>]
    static let Columns = "Columns"
    [<Literal>]
    static let Constraints = "Constraints"
    [<Literal>]
    static let Rows = "Rows"

    static let readColumns(reader: JsonReader, serializer: JsonSerializer, dataTable: DataTable) =
        reader.ReadAndAssert()
        reader.ValidatePropertyName(Columns, ObjectName)
        reader.ReadAndAssert()
        reader.ValidateJsonToken(JsonToken.StartArray, ObjectName)
        reader.ReadAndAssert();
        let dataColumnConverter = DataColumnConverter()
        while reader.TokenType <> JsonToken.EndArray do
            let dataColumn = dataColumnConverter.ReadJson(reader, typeof<DataColumn>, null, serializer) :?> DataColumn
            if dataTable.Columns.Contains(dataColumn.ColumnName) then // because we don't know which column it is before it is deserialized we need to copy property values if it already exists
                let existingDataColumn = dataTable.Columns.[dataColumn.ColumnName]
                existingDataColumn.AllowDBNull <- dataColumn.AllowDBNull
                existingDataColumn.AutoIncrement <- dataColumn.AutoIncrement
                existingDataColumn.AutoIncrementSeed <- dataColumn.AutoIncrementSeed
                existingDataColumn.AutoIncrementStep <- dataColumn.AutoIncrementStep
                existingDataColumn.Caption <- dataColumn.Caption
                existingDataColumn.ColumnMapping <- dataColumn.ColumnMapping
                existingDataColumn.DataType <- dataColumn.DataType
                existingDataColumn.DateTimeMode <- dataColumn.DateTimeMode
                if not existingDataColumn.AutoIncrement then
                    existingDataColumn.DefaultValue <- dataColumn.DefaultValue
                existingDataColumn.Expression <- dataColumn.Expression
                existingDataColumn.ExtendedProperties.ReplaceItems(dataColumn.ExtendedProperties)
                existingDataColumn.MaxLength <- dataColumn.MaxLength
                existingDataColumn.Namespace <- dataColumn.Namespace
                existingDataColumn.Prefix <- dataColumn.Prefix
                existingDataColumn.ReadOnly <- dataColumn.ReadOnly
            else
                dataTable.Columns.Add(dataColumn)
            reader.ReadAndAssert();
        reader.ValidateJsonToken(JsonToken.EndArray, ObjectName);

    static let readConstraints(reader: JsonReader, serializer: JsonSerializer, dataTable: DataTable) =
        reader.ReadAndAssert()
        reader.ValidatePropertyName(Constraints, ObjectName)
        reader.ReadAndAssert()
        reader.ValidateJsonToken(JsonToken.StartArray, ObjectName)
        reader.ReadAndAssert()
        let uniqueConstraintConverter = UniqueConstraintConverter(dataTable)
        while reader.TokenType <> JsonToken.EndArray do
            let uniqueConstraint = uniqueConstraintConverter.ReadJson(reader, typeof<UniqueConstraint>, null, serializer) :?> UniqueConstraint
            if dataTable.Constraints.Contains(uniqueConstraint.ConstraintName) then
                let existingUniqueConstraint = dataTable.Constraints.[uniqueConstraint.ConstraintName]
                existingUniqueConstraint.ExtendedProperties.ReplaceItems(uniqueConstraint.ExtendedProperties)
            else
                dataTable.Constraints.Add(uniqueConstraint)
        reader.ValidateJsonToken(JsonToken.EndArray, ObjectName)

    static let readRows(reader: JsonReader, serializer: JsonSerializer, dataTable: DataTable) =
        reader.ReadAndAssert()
        reader.ValidatePropertyName(Rows, ObjectName)
        reader.ReadAndAssert()
        reader.ValidateJsonToken(JsonToken.StartArray, ObjectName)
        reader.ReadAndAssert()
        let dataRowConverter = DataRowConverter(dataTable)
        while reader.TokenType <> JsonToken.EndArray do
            dataRowConverter.ReadJson(reader, typeof<DataRow>, null, serializer) |> ignore
        reader.ValidateJsonToken(JsonToken.EndArray, ObjectName)
        reader.ReadAndAssert()

    static let writeColumns(writer: JsonWriter, serializer: JsonSerializer, resolver: DefaultContractResolver, dataTable: DataTable) =
        writer.WritePropertyName(match resolver with | null -> Columns | _ -> resolver.GetResolvedPropertyName(Columns))
        writer.WriteStartArray()
        let dataColumnConverter = DataColumnConverter()
        for dataColumn in dataTable.Columns do
            dataColumnConverter.WriteJson(writer, dataColumn, serializer)
        writer.WriteEndArray()

    static let writeConstraints(writer: JsonWriter, serializer: JsonSerializer, resolver: DefaultContractResolver, dataTable: DataTable) =
        writer.WritePropertyName(match resolver with | null -> Constraints | _ -> resolver.GetResolvedPropertyName(Constraints))
        writer.WriteStartArray()
        let uniqueConstraintConverter = UniqueConstraintConverter(dataTable)
        for uniqueConstraint in dataTable.Constraints |> Seq.cast<Constraint> |> Seq.filter<Constraint>(fun tableConstraint -> tableConstraint :? UniqueConstraint) do
            uniqueConstraintConverter.WriteJson(writer, uniqueConstraint, serializer)
        writer.WriteEndArray()

    static let writeRows(writer: JsonWriter, serializer: JsonSerializer, resolver: DefaultContractResolver, dataTable: DataTable) =
        writer.WritePropertyName(match resolver with | null -> Rows | _ -> resolver.GetResolvedPropertyName(Rows))
        writer.WriteStartArray()
        let dataRowConverter = DataRowConverter(dataTable)
        for dataRow in dataTable.Rows do
            dataRowConverter.WriteJson(writer, dataRow, serializer)
        writer.WriteEndArray()

    override this.CanConvert(objectType) =
        typeof<DataTable>.IsAssignableFrom(objectType)

    override this.ReadJson(reader, objectType, existingValue, serializer) =
        if not (this.CanConvert(objectType)) then raise (new ArgumentOutOfRangeException("objectType", "Invalid object type"))
        if reader.TokenType = JsonToken.Null then
            null
        else
            let dataTable = 
                match existingValue with
                    | null -> new DataTable()
                    | _ -> existingValue :?> DataTable

            reader.ValidateJsonToken(JsonToken.StartObject, ObjectName)
            dataTable.CaseSensitive <- reader.ReadPropertyFromOutput<bool>(serializer, CaseSensitive, ObjectName)
            let displayExpression = reader.ReadPropertyFromOutput<string>(serializer, DisplayExpression, ObjectName)
            dataTable.Locale <- CultureInfo(reader.ReadPropertyFromOutput<string>(serializer, Locale, ObjectName))
            dataTable.MinimumCapacity <- reader.ReadPropertyFromOutput<int>(serializer, MinimumCapacity, ObjectName)
            dataTable.Namespace <- reader.ReadPropertyFromOutput<string>(serializer, Namespace, ObjectName)
            dataTable.Prefix <- reader.ReadPropertyFromOutput<string>(serializer, Prefix, ObjectName)
            dataTable.RemotingFormat <- reader.ReadPropertyFromOutput<SerializationFormat>(serializer, RemotingFormat, ObjectName)
            dataTable.TableName <- reader.ReadPropertyFromOutput<string>(serializer, TableName, ObjectName)
            readColumns(reader, serializer, dataTable)
            readConstraints(reader, serializer, dataTable)
            readRows(reader, serializer, dataTable)
            reader.ValidateJsonToken(JsonToken.EndObject, ObjectName)
            dataTable.DisplayExpression <- displayExpression
            dataTable :> obj

    override this.WriteJson(writer, value, serializer) =
        let dataTable = value :?> DataTable
        let resolver = serializer.ContractResolver :?> DefaultContractResolver

        writer.WriteStartObject()
        writer.WritePropertyToOutput(serializer, resolver, CaseSensitive, dataTable.CaseSensitive)
        writer.WritePropertyToOutput(serializer, resolver, DisplayExpression, dataTable.DisplayExpression)
        writer.WritePropertyToOutput(serializer, resolver, Locale, dataTable.Locale.Name)
        writer.WritePropertyToOutput(serializer, resolver, MinimumCapacity, dataTable.MinimumCapacity)
        writer.WritePropertyToOutput(serializer, resolver, Namespace, dataTable.Namespace)
        writer.WritePropertyToOutput(serializer, resolver, Prefix, dataTable.Prefix)
        writer.WritePropertyToOutput(serializer, resolver, RemotingFormat, dataTable.RemotingFormat)
        writer.WritePropertyToOutput(serializer, resolver, TableName, dataTable.TableName)
        writeColumns(writer, serializer, resolver, dataTable)
        writeConstraints(writer, serializer, resolver, dataTable)
        writeRows(writer, serializer, resolver, dataTable)
        writer.WriteEndObject()

