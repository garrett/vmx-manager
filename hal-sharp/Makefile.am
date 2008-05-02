ASSEMBLY_NAME = Hal
ASSEMBLY = $(ASSEMBLY_NAME).dll

haldir = $(pkglibdir)
hal_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

ASSEMBLY_SOURCES = \
	$(srcdir)/Manager.cs \
	$(srcdir)/Device.cs \
	$(srcdir)/Volume.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) -debug -target:library -out:$@ $(VMXMAN_LIBS) -pkg:ndesk-dbus-1.0 $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES)
CLEANFILES = $(hal_SCRIPTS) *.pidb
MAINTAINERCLEANFILES = Makefile.in

