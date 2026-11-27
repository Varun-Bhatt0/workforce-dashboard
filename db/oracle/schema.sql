-- Workforce Analytics — Oracle schema
-- Written for Oracle 19c and later. Run as the schema owner, then load data
-- (sample_data.sql for the synthetic dataset, or your own HR extract).
--
-- The application only reads these tables. Give the account it connects with SELECT
-- on unit_mas and manpower_data and nothing else (see the GRANT at the end).

CREATE TABLE unit_mas (
    unit_cd     NUMBER(6)      NOT NULL,
    unit_abb    VARCHAR2(10)   NOT NULL,
    unit_name   VARCHAR2(100)  NOT NULL,
    unit_group  VARCHAR2(50)   DEFAULT 'Other units' NOT NULL,
    sort_order  NUMBER(4)      DEFAULT 999 NOT NULL,
    active      CHAR(1)        DEFAULT 'Y' NOT NULL,
    CONSTRAINT unit_mas_pk      PRIMARY KEY (unit_cd),
    CONSTRAINT unit_mas_abb_uk  UNIQUE (unit_abb),
    CONSTRAINT unit_mas_active_ck CHECK (active IN ('Y', 'N'))
);

-- One row per unit, snapshot date and cadre.
-- year_date is a real DATE (1 April of the year), not text, so it can be indexed and compared.
CREATE TABLE manpower_data (
    unit_cd            NUMBER(6)     NOT NULL,
    year_date          DATE          NOT NULL,
    cadre              VARCHAR2(20)  NOT NULL,
    manpower_count     NUMBER(7)     NOT NULL,
    works              NUMBER(7)     DEFAULT 0 NOT NULL,
    non_works          NUMBER(7)     DEFAULT 0 NOT NULL,
    projects           NUMBER(7)     DEFAULT 0 NOT NULL,
    mines              NUMBER(7)     DEFAULT 0 NOT NULL,
    recruitments       NUMBER(7)     DEFAULT 0 NOT NULL,
    separations        NUMBER(7)     DEFAULT 0 NOT NULL,
    average_age        NUMBER(4,1),
    male               NUMBER(7)     DEFAULT 0 NOT NULL,
    female             NUMBER(7)     DEFAULT 0 NOT NULL,
    post_graduate      NUMBER(7)     DEFAULT 0 NOT NULL,
    graduate           NUMBER(7)     DEFAULT 0 NOT NULL,
    diploma            NUMBER(7)     DEFAULT 0 NOT NULL,
    iti                NUMBER(7)     DEFAULT 0 NOT NULL,
    higher_secondary   NUMBER(7)     DEFAULT 0 NOT NULL,
    matriculate        NUMBER(7)     DEFAULT 0 NOT NULL,
    below_matriculate  NUMBER(7)     DEFAULT 0 NOT NULL,
    CONSTRAINT manpower_data_pk    PRIMARY KEY (unit_cd, year_date, cadre),
    CONSTRAINT manpower_data_unit_fk FOREIGN KEY (unit_cd) REFERENCES unit_mas (unit_cd),
    CONSTRAINT manpower_data_cadre_ck CHECK (cadre IN ('Executive', 'Non-Executive')),
    CONSTRAINT manpower_data_nonneg_ck CHECK (
        manpower_count >= 0 AND works >= 0 AND non_works >= 0 AND projects >= 0 AND mines >= 0
        AND recruitments >= 0 AND separations >= 0 AND male >= 0 AND female >= 0),
    -- The dashboard's breakdowns assume these add up; reject rows that don't.
    CONSTRAINT manpower_data_function_ck CHECK (works + non_works + projects + mines = manpower_count),
    CONSTRAINT manpower_data_gender_ck   CHECK (male + female = manpower_count),
    CONSTRAINT manpower_data_qual_ck     CHECK (post_graduate + graduate + diploma + iti
                                               + higher_secondary + matriculate + below_matriculate = manpower_count)
);

CREATE INDEX manpower_data_year_ix ON manpower_data (year_date);

-- Convenience view for ad-hoc reporting: one row per unit and year, both cadres combined.
CREATE OR REPLACE VIEW manpower_by_unit_year AS
SELECT u.unit_abb,
       u.unit_name,
       u.unit_group,
       EXTRACT(YEAR FROM m.year_date) AS snapshot_year,
       SUM(m.manpower_count)          AS manpower,
       SUM(m.works)                   AS works,
       SUM(m.non_works)               AS non_works,
       SUM(m.recruitments)            AS recruitments,
       SUM(m.separations)             AS separations,
       ROUND(SUM(m.average_age * m.manpower_count) / NULLIF(SUM(m.manpower_count), 0), 1) AS average_age
  FROM manpower_data m
  JOIN unit_mas u ON u.unit_cd = m.unit_cd
 GROUP BY u.unit_abb, u.unit_name, u.unit_group, EXTRACT(YEAR FROM m.year_date);

-- Read-only access for the application account (replace WORKFORCE_APP with yours):
-- GRANT SELECT ON unit_mas TO workforce_app;
-- GRANT SELECT ON manpower_data TO workforce_app;
