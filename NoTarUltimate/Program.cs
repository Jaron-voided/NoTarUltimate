// See https://aka.ms/new-console-template for more information


using NoTarUltimate;

string input = "/home/zeref-dragneel/Desktop/Notar/NoTarUltimate/NoTarUltimate/TestFiles";
string notar = "./TestFilesPacked.notar";
// NotarPackage notarPackage = new NotarPackage();
// notarPackage.FromDirectory(input);
// notarPackage.Pack(notar);

new NotarPackage()
    .FromDirectory(input)
    .Pack(notar);