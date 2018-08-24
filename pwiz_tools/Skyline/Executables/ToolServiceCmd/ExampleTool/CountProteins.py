#!/usr/bin/python

import os
import sys
import subprocess

report_proteinlocators = """<?xml version="1.0"?>
<views>
  <view name="ProteinLocators" rowsource="pwiz.Skyline.Model.Databinding.Entities.Protein" sublist="Results!*">
    <column name="Locator" />
  </view>
</views>"""

connectionname = sys.argv[1]
toolservicecmdexe = os.path.dirname(os.path.realpath(__file__)) + "/ToolServiceCmd.exe"
p = subprocess.Popen([toolservicecmdexe, 'GetReport', '--connectionname', connectionname], stdin=subprocess.PIPE, stdout=subprocess.PIPE)
(report_text, error_text) = p.communicate(input=report_proteinlocators)

print "There are ", report_text.count('\n') - 1, " proteins"
input("Press Enter to continue")
