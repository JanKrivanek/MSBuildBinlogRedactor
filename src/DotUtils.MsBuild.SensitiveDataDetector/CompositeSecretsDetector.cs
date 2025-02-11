﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DotUtils.MsBuild.SensitiveDataDetector;

namespace Microsoft.Build.SensitiveDataDetector;

internal class CompositeSecretsDetector : ISensitiveDataRedactor, ISensitiveDataDetector
{
    private readonly ISensitiveDataDetector[] _detectors;

    private readonly ISensitiveDataRedactor[] _redactors;

    public CompositeSecretsDetector(params ISensitiveDataRedactor[] redactors) => _redactors = redactors;

    public CompositeSecretsDetector(params ISensitiveDataDetector[] detectors) => _detectors = detectors;

    public Dictionary<SensitiveDataKind, List<SecretDescriptor>> Detect(string input)
    {
        Dictionary<SensitiveDataKind, List<SecretDescriptor>> result = new();
        foreach (ISensitiveDataDetector detector in _detectors)
        {
            Dictionary<SensitiveDataKind, List<SecretDescriptor>> detectorResult = detector.Detect(input);

            foreach (var kvp in detectorResult)
            {
                if (result.ContainsKey(kvp.Key))
                {
                    result[kvp.Key].AddRange(kvp.Value);
                }
                else
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
        }

        return result;
    }

    public string Redact(string input)
    {
        foreach (ISensitiveDataRedactor redactor in _redactors)
        {
            input = redactor.Redact(input);
        }

        return input;
    }
}

