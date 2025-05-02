// See https://aka.ms/new-console-template for more information

using NoTarUltimate;


string input = "/home/zeref-dragneel/RiderProjects/Notar/NoTarUltimate/NoTarUltimate/TestFiles";
string notar = "./TestFilesPacked.notar";
string directoryToUnpackTo = "/home/zeref-dragneel/RiderProjects/Notar/NoTarUltimate/NoTarUltimate/TestFilesPacked";


NotarPackage package = new NotarPackage()
    .FromDirectory(input);
package.Pack(notar);

NotarPackage.ToDirectory(notar,directoryToUnpackTo);
    