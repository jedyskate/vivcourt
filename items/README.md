# Order Book Processor

This application processes a binary stream of order book messages and generates a human-readable price depth snapshot for the top N levels.

## Usage

The application can process data from a file or from standard input (`stdin`).


```shell
  dotnet run --project src/OrderBookApp/OrderBookApp.csproj -- <input_file_path> <N>
```
-   `<input_file_path>`: The path to the binary input stream file.
-   `<N>`: The number of price depth levels to display.

**Example:**
```shell
  dotnet run --project src/OrderBookApp/OrderBookApp.csproj -- items/input1.stream 5
```
![Test image.png](Test%20image.png)

## Build

To build the application, run the following command from the root directory:

```shell
  dotnet build
```

## Test

To run the unit tests, run the following command from the root directory:

```shell
  dotnet test
```
