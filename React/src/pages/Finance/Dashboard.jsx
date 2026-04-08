import { useEffect, useState, useCallback, useMemo } from 'react';
import { Card, Col, Row, Statistic, Spin, Typography, Button, Space, Table } from 'antd';
import { LeftOutlined, RightOutlined } from '@ant-design/icons';
import {
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  Legend,
  CartesianGrid,
  PieChart,
  Pie,
  Cell,
} from 'recharts';
import dayjs from 'dayjs';
import 'dayjs/locale/pt-br';
import { financeApi } from '@services/financeApi';
import { App } from 'antd';

dayjs.locale('pt-br');

const { Title, Text } = Typography;

const formatMoney = (v) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(Number(v || 0));

/** Alinha com o backend (DashboardService.CategoryLabel + comparador de categoria). */
function categoryKey(name) {
  const s = name != null && String(name).trim() !== '' ? String(name).trim() : 'Outros';
  return s.toLowerCase();
}

function expensesForCategory(expenseLines, categoryName) {
  const want = categoryKey(categoryName);
  return (expenseLines || []).filter((e) => categoryKey(e.categoryName) === want);
}

const BREAKDOWN_KIND_LABELS = {
  expense: 'Despesa',
  installment: 'Parcela',
  recurring: 'Recorrente',
};

const PIE_COLORS = [
  '#1677ff',
  '#52c41a',
  '#faad14',
  '#ff7875',
  '#722ed1',
  '#13c2c2',
  '#eb2f96',
  '#fa8c16',
  '#2f54eb',
  '#a0d911',
];

const Dashboard = () => {
  const { message } = App.useApp();
  const [loading, setLoading] = useState(true);
  const [data, setData] = useState(null);
  const [selectedMonth, setSelectedMonth] = useState(() => dayjs().startOf('month'));
  const [expenseLines, setExpenseLines] = useState([]);
  const [expenseLinesLoading, setExpenseLinesLoading] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const d = await financeApi.dashboard(selectedMonth.year(), selectedMonth.month() + 1);
      setData(d);
    } catch (e) {
      message.error(e.message || 'Falha ao carregar dashboard');
    } finally {
      setLoading(false);
    }
  }, [message, selectedMonth]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    let cancelled = false;
    setExpenseLinesLoading(true);
    setExpenseLines([]);
    (async () => {
      try {
        const list = await financeApi.expenses(selectedMonth.year(), selectedMonth.month() + 1);
        if (!cancelled) setExpenseLines(Array.isArray(list) ? list : []);
      } catch (e) {
        if (!cancelled) {
          message.error(e.message || 'Erro ao carregar lançamentos');
          setExpenseLines([]);
        }
      } finally {
        if (!cancelled) setExpenseLinesLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [selectedMonth, message]);

  const goPrevMonth = () => setSelectedMonth((m) => m.subtract(1, 'month').startOf('month'));
  const goNextMonth = () => setSelectedMonth((m) => m.add(1, 'month').startOf('month'));

  /** Permite ver meses futuros (parcelas, recorrentes, projeções). Limite evita navegação infinita. */
  const MAX_MONTHS_AHEAD = 120;
  const canGoNext = selectedMonth.isBefore(dayjs().add(MAX_MONTHS_AHEAD, 'month'), 'month');
  const monthLabel = selectedMonth.format('MMMM [de] YYYY');

  const chartData = (data?.lastMonthsFlow || []).map((m, idx) => ({
    name:
      idx === 0
        ? `${m.label} (anterior)`
        : idx === 1
          ? `${m.label} (atual)`
          : idx === 2
            ? `${m.label} (próximo)`
            : m.label,
    Renda: Number(m.income),
    Gastos: Number(m.expense),
  }));

  const pieData = (data?.monthExpensesByCategory || []).map((x) => ({
    name: x.categoryName,
    value: Number(x.amount),
  }));

  const categoryTableRows = useMemo(() => {
    const rows = data?.monthExpensesByCategory || [];
    const total = Number(data?.monthExpense) || 0;
    return rows.map((item, i) => {
      const amt = Number(item.amount) || 0;
      return {
        key: `${item.categoryName ?? 'cat'}-${i}`,
        categoryName: item.categoryName,
        amount: amt,
        pct: total > 0 ? (amt / total) * 100 : 0,
        breakdownItems: Array.isArray(item.items) ? item.items : [],
      };
    });
  }, [data]);

  const categoryByExpenseColumns = [
    {
      title: 'Categoria',
      dataIndex: 'categoryName',
      key: 'categoryName',
      ellipsis: true,
      render: (t) => t || '—',
    },
    {
      title: '% do total',
      dataIndex: 'pct',
      key: 'pct',
      width: 108,
      align: 'right',
      render: (pct) => (pct >= 0.05 ? `${pct.toFixed(1)}%` : '—'),
    },
    {
      title: 'Valor',
      dataIndex: 'amount',
      key: 'amount',
      width: 120,
      align: 'right',
      render: (v) => formatMoney(v),
    },
  ];

  const expenseLinesColumns = [
    {
      title: 'Data',
      dataIndex: 'date',
      width: 96,
      render: (d) => (d ? dayjs(d).format('DD/MM/YYYY') : '—'),
    },
    {
      title: 'Categoria',
      dataIndex: 'categoryName',
      width: 140,
      ellipsis: true,
      render: (t) => t || '—',
    },
    {
      title: 'Descrição',
      dataIndex: 'description',
      ellipsis: true,
      render: (t) => (t && String(t).trim() ? t : '—'),
    },
    {
      title: 'Valor',
      dataIndex: 'amount',
      width: 108,
      align: 'right',
      render: (v) => formatMoney(v),
    },
  ];

  const categoryBreakdownColumns = [
    {
      title: 'Tipo',
      dataIndex: 'kind',
      key: 'kind',
      width: 100,
      render: (k) => BREAKDOWN_KIND_LABELS[k] || k || '—',
    },
    {
      title: 'Data',
      dataIndex: 'date',
      key: 'date',
      width: 96,
      render: (d) => (d ? dayjs(d).format('DD/MM/YYYY') : '—'),
    },
    {
      title: 'Descrição',
      dataIndex: 'title',
      key: 'title',
      ellipsis: true,
      render: (t) => (t && String(t).trim() ? t : '—'),
    },
    {
      title: 'Valor',
      dataIndex: 'amount',
      key: 'amount',
      width: 108,
      align: 'right',
      render: (v) => formatMoney(v),
    },
  ];

  return (
    <div>
      <Space direction="vertical" size={8} style={{ width: '100%', marginBottom: 16 }}>
        <Title level={3} style={{ marginTop: 0, marginBottom: 0 }}>
          Visão geral
        </Title>
        <Space align="center" wrap>
          <Button
            type="text"
            icon={<LeftOutlined />}
            onClick={goPrevMonth}
            aria-label="Mês anterior"
          />
          <Text strong style={{ fontSize: 16, minWidth: 200, textAlign: 'center', display: 'inline-block' }}>
            {monthLabel}
          </Text>
          <Button
            type="text"
            icon={<RightOutlined />}
            onClick={goNextMonth}
            disabled={!canGoNext}
            aria-label="Próximo mês"
          />
        </Space>
      </Space>
      <Spin spinning={loading}>
        <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic title="Renda no mês" value={data?.monthIncome} formatter={formatMoney} />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title={`Gastos no mês (${selectedMonth.format('MM/YYYY')})`}
              value={data?.monthExpense}
              formatter={formatMoney}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="Saldo do mês"
              value={data?.monthBalance}
              formatter={formatMoney}
              valueStyle={{ color: (data?.monthBalance || 0) >= 0 ? '#3f8600' : '#cf1322' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic title="Patrimônio líquido (contas)" value={data?.totalPatrimony} formatter={formatMoney} />
          </Card>
        </Col>
     
      </Row>
      <Row gutter={[16, 16]} style={{ marginTop: 24 }}>
        <Col xs={24} lg={14}>
          <Card
            title={`Renda x gastos — mês anterior, atual (${selectedMonth.format('MM/YYYY')}) e próximo`}
          >
            <div style={{ width: '100%', height: 340 }}>
              <ResponsiveContainer>
                <BarChart data={chartData} margin={{ top: 8, right: 8, left: 0, bottom: 8 }}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="name" interval={0} angle={-22} textAnchor="end" height={56} />
                  <YAxis />
                  <Tooltip formatter={(v) => formatMoney(v)} />
                  <Legend />
                  <Bar dataKey="Renda" fill="#52c41a" />
                  <Bar dataKey="Gastos" fill="#ff7875" />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </Card>
        </Col>
        <Col xs={24} lg={10}>
          <Card title={`Gastos do mês por categoria — ${selectedMonth.format('MM/YYYY')}`}>
            {pieData.length === 0 ? (
              <div style={{ height: 340, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                <Text type="secondary">Nenhum gasto neste mês</Text>
              </div>
            ) : (
              <div style={{ width: '100%', height: 340 }}>
                <ResponsiveContainer>
                  <PieChart>
                    <Pie
                      data={pieData}
                      dataKey="value"
                      nameKey="name"
                      cx="50%"
                      cy="50%"
                      outerRadius={118}
                      paddingAngle={1}
                      label={({ percent }) =>
                        percent >= 0.06 ? `${(percent * 100).toFixed(0)}%` : ''
                      }
                    >
                      {pieData.map((_, index) => (
                        <Cell key={`slice-${index}`} fill={PIE_COLORS[index % PIE_COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip formatter={(v) => formatMoney(v)} />
                    <Legend layout="horizontal" verticalAlign="bottom" />
                  </PieChart>
                </ResponsiveContainer>
              </div>
            )}
          </Card>
        </Col>
      </Row>
      <Row gutter={[16, 16]} style={{ marginTop: 24 }}>
        <Col xs={24} lg={12}>
          <Card title={`Gastos por categoria — ${selectedMonth.format('MM/YYYY')}`}>

            <Table
              size="small"
              pagination={false}
              scroll={{ x: true }}
              rowKey="key"
              dataSource={categoryTableRows}
              locale={{ emptyText: 'Nenhuma categoria neste mês' }}
              columns={categoryByExpenseColumns}
              expandable={{
                expandedRowRender: (record) => {
                  const apiItems = record.breakdownItems || [];
                  if (apiItems.length > 0) {
                    return (
                      <div style={{ padding: '8px 8px 12px 40px', background: '#fafafa' }}>
                        <Table
                          size="small"
                          pagination={false}
                          rowKey={(_, idx) => `bd-${record.key}-${idx}`}
                          dataSource={apiItems}
                          columns={categoryBreakdownColumns}
                          locale={{ emptyText: '—' }}
                        />
                      </div>
                    );
                  }
                  if (expenseLinesLoading) {
                    return (
                      <div style={{ padding: '16px 24px', background: '#fafafa' }}>
                        <Spin size="small" />{' '}
                        <Text type="secondary">Carregando lançamentos…</Text>
                      </div>
                    );
                  }
                  const lines = expensesForCategory(expenseLines, record.categoryName);
                  return (
                    <div style={{ padding: '8px 8px 12px 40px', background: '#fafafa' }}>
                      {lines.length === 0 ? (
                        <Text type="secondary">
                          Nenhum detalhe disponível. Atualize a API ou verifique se o valor vem só de regras
                          antigas de resumo.
                        </Text>
                      ) : (
                        <Table
                          size="small"
                          pagination={false}
                          rowKey="id"
                          dataSource={lines}
                          columns={[
                            {
                              title: 'Data',
                              dataIndex: 'date',
                              width: 96,
                              render: (d) => (d ? dayjs(d).format('DD/MM/YYYY') : '—'),
                            },
                            {
                              title: 'Descrição',
                              dataIndex: 'description',
                              ellipsis: true,
                              render: (t) => (t && String(t).trim() ? t : '—'),
                            },
                            {
                              title: 'Valor',
                              dataIndex: 'amount',
                              width: 108,
                              align: 'right',
                              render: (v) => formatMoney(v),
                            },
                          ]}
                          locale={{ emptyText: '—' }}
                        />
                      )}
                    </div>
                  );
                },
              }}
            />
          </Card>
        </Col>
        <Col xs={24} lg={12}>
          <Card title={`Lançamentos registrados — ${selectedMonth.format('MM/YYYY')}`}>
            <Spin spinning={expenseLinesLoading}>
              <Table
                size="small"
                pagination={false}
                scroll={{ x: true }}
                rowKey="id"
                dataSource={expenseLines}
                locale={{ emptyText: 'Nenhum lançamento neste mês' }}
                columns={expenseLinesColumns}
              />
            </Spin>
          </Card>
        </Col>
      </Row>
      </Spin>
    </div>
  );
};

export default Dashboard;
