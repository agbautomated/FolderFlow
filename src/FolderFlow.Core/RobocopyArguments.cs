namespace FolderFlow.Core;

public static class RobocopyArguments
{
    public static IReadOnlyList<string> Build(ValidatedJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        CopySettings settings = job.Job.Settings;
        List<string> args = [job.SourcePath, job.DestinationPath, "*", "/E"];
        // /E + /PURGE matches files without /MIR's additional root security behavior.
        if (job.Job.Mode == CopyMode.Mirror) args.Add("/PURGE");
        args.AddRange(["/COPY:DAT", "/DCOPY:DAT", "/XJ", "/XJD", "/XJF",
            $"/R:{settings.RetryCount}", $"/W:{settings.RetryWaitSeconds}", "/BYTES", "/FP"]);
        if (settings.Multithreaded) args.Add($"/MT:{settings.ThreadCount}");
        args.Add(settings.LargeFiles ? "/J" : "/Z");
        if (job.Job.DryRun) args.Add("/L");
        return args.AsReadOnly();
    }
}
