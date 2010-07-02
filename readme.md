Xsd Modelling Helper
===================

This command line tool is used to extract the information about leaf level nodes from a schema. 
As such it is able to generate a CSV file with the following information:
 * Node - Parent/Element name of the node
 * XPath - The Full xpath to the node - e.g. /Root/Child/Value
 * Annotation - The annotation associated with the node
 * Data Type - Either a standard type, e.g. String, or a custom type, e.g. ResultComplexType
 * Optional - Whether or not the element has a MinOccurs of zero.
 * Custom Attributes - Additional custom attributes found on nodes, e.g. fpml-annotation:deprecated

It will also allow you to update the schema from a modified version of the extracted csv file. So someone who
is not familiar with editing XSD schemas can open up the csv file in Excel, edit the annotations and then run
the tool to have that update the annotations within the schema.

Please note that the moment it focuses on elements in the schema and as such attribute support, while present needs
more testing and fleshing out.

Debugging
---------
To debug the console application add the following Start Options > Command line arguments:
-s="..\..\..\Sample\fpml-main-4-8.xsd" -t="XsdInfo.csv" -r:"http://www.fpml.org/2010/FpML-4-8:DataDocument" -i=validation -a=fpml-annotation:deprecated -a=fpml-annotation:deprecatedReason -a=ecore:reference

This will generate a csv file in the bing\debug directory of the generated xpaths. Note that because DataDocument is not an element it
is not included as the root of the XPaths that are generated.

Prerequisites
-------------
# The test project requires NUnit-2.5.5 which can be downloaded from http://www.nunit.org/index.php?p=download.

